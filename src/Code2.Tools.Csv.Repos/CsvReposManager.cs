using Code2.Tools.Csv.Repos.Internals;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos;

public class CsvReposManager : ICsvReposManager
{
	public CsvReposManager(ICsvReaderFactory csvReaderFactory) : this(csvReaderFactory, new ReflectionUtility(), new FileSystem())
	{ }
	internal CsvReposManager(ICsvReaderFactory csvReaderFactory, IReflectionUtility reflectionUtility, IFileSystem fileSystem)
	{
		_csvReaderFactory = csvReaderFactory;
		_reflectionUtility = reflectionUtility;
		_fileSystem = fileSystem;
	}

	private readonly ICsvReaderFactory _csvReaderFactory;
	private readonly IReflectionUtility _reflectionUtility;
	private readonly IFileSystem _fileSystem;

	private CsvReaderOptions? _defaultReaderOptions;
	private int _updateIntervalInMinutes = 5;
	private int _retryIntervalInMinutes = 60;
	private int _csvReadSize = 5000;
	private Timer? _updateTimer;

	public CsvFileInfo[] Files { get; private set; } = Array.Empty<CsvFileInfo>();
	public ICsvUpdateTask[] UpdateTasks { get; private set; } = Array.Empty<ICsvUpdateTask>();
	public bool IsAutoUpdating => _updateTimer is not null;
	public Action<IResult>? UpdateTaskError { get; set; }

	public async Task LoadAsync(Type[]? targetItemTypes = null, CancellationToken cancellationToken = default)
	{
		if (Files.Length == 0) return;

		await Task.Run(() =>
		{
			var filesToLoad = Files.Where(x => targetItemTypes is null || targetItemTypes.Contains(x.ItemType)).ToArray();
			foreach (var fileInfo in filesToLoad)
			{
				_reflectionUtility.InvokePrivateGenericMethod(this, nameof(LoadRepository), fileInfo.ItemType, new object[] { fileInfo.FullFilePath, fileInfo.Repository, fileInfo.CsvReaderOptions! });
			}
		}, cancellationToken);
	}

	public async Task UpdateAsync(CancellationToken cancellationToken = default)
	{
		if (UpdateTasks.Length == 0) return;
		DateTime now = DateTime.Now;
		var tasksToRun = UpdateTasks.Where(x => !(x.IsDisabled || x.IsRunning) && x.RunAfter <= now).ToArray();

		var resultTasks = tasksToRun.AsParallel().Select(async x =>
		{
			var result = await TryRunAsync(x, cancellationToken);
			if (result.IsSuccess && x.AffectedTypes is not null) await LoadAsync(x.AffectedTypes, cancellationToken);
			x.RunAfter = now.AddMinutes(result.IsSuccess ? x.IntervalInMinutes : x.RetryIntervalInMinutes ?? _retryIntervalInMinutes);
			return result;
		}).ToArray();

		var results = await Task.WhenAll(resultTasks);

		var errorResults = results.Where(x => x.State == ResultState.Error).ToArray();
		foreach (var errorResult in errorResults)
		{
			OnTaskError(errorResult);
		}
	}

	public void Configure(Action<CsvReposOptions> config, IServiceProvider? serviceProvider = null)
	{
		var options = new CsvReposOptions();
		config(options);
		Configure(options, serviceProvider);
	}

	public void Configure(CsvReposOptions options, IServiceProvider? serviceProvider = null)
	{
		if (options.UpdateIntervalInMinutes.HasValue) _updateIntervalInMinutes = options.UpdateIntervalInMinutes.Value;
		if (options.RetryIntervalInMinutes.HasValue) _retryIntervalInMinutes = options.RetryIntervalInMinutes.Value;
		if (options.CsvReadSize is not null) _csvReadSize = options.CsvReadSize.Value;
		if (options.DefaultReaderOptions is not null) _defaultReaderOptions = _reflectionUtility.GetShallowCopy(options.DefaultReaderOptions);
		if (options.Files is not null) Files = options.Files.Select(x => CreateCsvFileInfo(x, serviceProvider)).ToArray();
		if (options.UpdateTasks is not null) UpdateTasks = options.UpdateTasks.Select(x => CreateUpdateTask(x, serviceProvider)).ToArray();

		if (_updateIntervalInMinutes > 0 && UpdateTasks.Length > 0)
		{
			int intervalInSeconds = _updateIntervalInMinutes * 60;
			int dueTimeMs = 1000 * (intervalInSeconds - (((int)DateTime.Now.TimeOfDay.TotalSeconds) % intervalInSeconds));
			_updateTimer = new Timer(new TimerCallback(OnUpdateTimer), null, dueTimeMs, intervalInSeconds * 1000);
		}
		else
		{
			_updateTimer?.Dispose();
		}
	}

	private static async Task<IResult> TryRunAsync(ICsvUpdateTask updateTask, CancellationToken cancellationToken)
	{
		try
		{
			return await updateTask.RunAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			return Result.Error($"Update failed for {updateTask.GetType().Name}", ex);
		}
	}

	private CsvFileInfo CreateCsvFileInfo(CsvFileOptions fileOptions, IServiceProvider? serviceProvider = null)
	{
		Type? itemType = fileOptions.TypeName is null? fileOptions.Type: _reflectionUtility.GetRequiredClassType(fileOptions.TypeName);
		Type repoInterfaceType = _reflectionUtility.GetRepositoryInterfaceType(fileOptions.Repository, itemType);
		object? repo = _reflectionUtility.GetOrCreateRepository(fileOptions.Repository, itemType, serviceProvider);

		string fullFilePath = _fileSystem.PathGetFullPath(fileOptions.FilePath);
		var readerOptions = fileOptions.ReaderOptions ?? _defaultReaderOptions;
		if (readerOptions is not null) readerOptions = _reflectionUtility.GetShallowCopy(readerOptions);

		return new CsvFileInfo(fullFilePath, repo!, repoInterfaceType!.GetGenericArguments()[0], readerOptions);
	}

	private ICsvUpdateTask CreateUpdateTask(CsvUpdateTaskOptions options, IServiceProvider? serviceProvider = null)
	{
		Type? taskType = options.TaskTypeName is null ? options.TaskType: _reflectionUtility.GetRequiredClassType(options.TaskTypeName);
		if (taskType is null) throw new InvalidOperationException("Task type not defined");

		ICsvUpdateTask instance = (ICsvUpdateTask)_reflectionUtility.ActivatorCreateInstance(taskType, serviceProvider);
		instance.IsDisabled = options.IsDisabled;
		instance.IntervalInMinutes = options.IntervalInMinutes;
		instance.RetryIntervalInMinutes = options.RetryIntervalInMinutes;
		instance.AffectedTypes = options.AffectedTypeNames?.Select(_reflectionUtility.GetRequiredClassType).ToArray();
		if (options.Properties is not null) _reflectionUtility.SetValueTypeOrStringProperties(instance, options.Properties);

		return instance;
	}

	private void LoadRepository<T>(string fullFilePath, ICsvRepository<T> repository, CsvReaderOptions? csvReaderOptions = null) where T : class, new()
	{
		repository.Clear();
		using var reader = _csvReaderFactory.Create<T>(fullFilePath, csvReaderOptions);
		while (!reader.EndOfStream)
		{
			repository.Add(reader.ReadObjects(_csvReadSize));
		}
	}

	private async void OnUpdateTimer(object? state)
		=> await UpdateAsync();

	private void OnTaskError(IResult errorResult)
	{
		if (UpdateTaskError is null) throw new InvalidOperationException(errorResult.Message, errorResult.SourceException);
		UpdateTaskError(errorResult);
	}




}
