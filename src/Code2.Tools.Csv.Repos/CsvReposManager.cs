using Code2.Tools.Csv.Repos.Internals;
using Microsoft.Extensions.DependencyInjection;
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
	private int _readerReadSize = 5000;
	private Timer? _updateTimer;
	private IServiceProvider? _serviceProvider;
	private CsvUpdateTaskOptions[]? _updateTaskOptions;

	public ICsvFileInfo[] Files { get; private set; } = Array.Empty<ICsvFileInfo>();
	public ICsvUpdateTask[] UpdateTasks { get; private set; } = Array.Empty<ICsvUpdateTask>();
	public bool IsAutoUpdating => _updateTimer is not null;
	public event EventHandler<ResultEventArgs>? UpdateTaskError;
	public event EventHandler<UnhandledExceptionEventArgs>? ReaderError;
	public event EventHandler<DataLoadedEventArgs>? DataLoaded;

	public async Task LoadAsync(Type[]? targetItemTypes = null, CancellationToken cancellationToken = default)
	{
		if (_serviceProvider is null) throw new InvalidOperationException("Service provider not configured");
		if (Files.Length == 0) return;

		await Task.Run(() =>
		{
			var filesToLoad = Files.Where(x => targetItemTypes is null || targetItemTypes.Contains(x.ItemType)).OrderBy(x => x.ItemType!.Name).ToArray();
			string? previousTypeName = null;
			foreach (var fileInfo in filesToLoad)
			{
				bool clearRepository = previousTypeName is null || fileInfo.ItemType!.Name != previousTypeName;
				_reflectionUtility.InvokePrivateGenericMethod(this, nameof(LoadRepository), fileInfo.ItemType!, new object?[] { fileInfo.FilePath, clearRepository, fileInfo.ReaderOptions ?? _defaultReaderOptions });
				previousTypeName = fileInfo.ItemType!.Name;
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

	public void Configure(Action<CsvReposOptions> config)
	{
		var options = new CsvReposOptions();
		config(options);
		Configure(options);
	}

	public void Configure(CsvReposOptions options)
	{
		if (options.UpdateIntervalInMinutes.HasValue) _updateIntervalInMinutes = options.UpdateIntervalInMinutes.Value;
		if (options.RetryIntervalInMinutes.HasValue) _retryIntervalInMinutes = options.RetryIntervalInMinutes.Value;
		if (options.ReaderReadSize is not null) _readerReadSize = options.ReaderReadSize.Value;
		if (options.DefaultReaderOptions is not null) _defaultReaderOptions = _reflectionUtility.GetShallowCopy(options.DefaultReaderOptions);
		if (options.Files is not null) Files = options.Files.Select(GetCopyWithResolvedTypes).ToArray();
		if (options.UpdateTasks is not null) _updateTaskOptions = options.UpdateTasks.Select(CopyUpdateTaskOptions).ToArray();
		if (options.ServiceProvider is not null) _serviceProvider = options.ServiceProvider;
		if (options.OnUpdateTaskError is not null) UpdateTaskError += (_, e) => options.OnUpdateTaskError(e);
		if (options.OnReaderError is not null) ReaderError += (_, e) => options.OnReaderError(e);
		if (options.OnDataLoaded is not null) DataLoaded += (_, e) => options.OnDataLoaded(e);

		if (options.ServiceCollection is not null) AddFileRepositoriesToServiceCollection(options.ServiceCollection, Files);
		if (_serviceProvider is not null && _updateTaskOptions is not null) UpdateTasks = _updateTaskOptions.Select(CreateUpdateTask).ToArray();

		CreateOrDestroyUpdateTimer(_updateIntervalInMinutes);
	}

	private CsvUpdateTaskOptions CopyUpdateTaskOptions(CsvUpdateTaskOptions options)
	{
		var newOptions = _reflectionUtility.GetShallowCopy(options);
		if (options.Properties is not null) newOptions.Properties = options.Properties.ToDictionary(x => x.Key, x => x.Value);
		if (options.TaskType is not null) newOptions.TaskType = options.TaskType;
		return newOptions;
	}

	private void AddFileRepositoriesToServiceCollection(IServiceCollection services, ICsvFileInfo[] files)
	{
		foreach (var fileInfo in files)
		{
			Type repoInterfaceType = _reflectionUtility.TypeMakeGeneric(typeof(ICsvRepository<>), fileInfo.ItemType!);

			if (fileInfo.IsTransientRepository)
			{
				services.AddTransient(repoInterfaceType, fileInfo.RepositoryType!);
			}
			else
			{
				services.AddSingleton(repoInterfaceType, fileInfo.RepositoryType!);
			}
		}
	}

	private void CreateOrDestroyUpdateTimer(int updateIntervalInMinutes)
	{
		_updateTimer?.Dispose();
		_updateTimer = null;
		if (updateIntervalInMinutes > 0)
		{
			int intervalInSeconds = _updateIntervalInMinutes * 60;
			int dueTimeMs = 1000 * (intervalInSeconds - (((int)DateTime.Now.TimeOfDay.TotalSeconds) % intervalInSeconds));
			_updateTimer = new Timer(new TimerCallback(OnUpdateTimer), null, dueTimeMs, intervalInSeconds * 1000);
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

	private CsvFileOptions GetCopyWithResolvedTypes(CsvFileOptions fileOptions)
	{
		var options = _reflectionUtility.GetShallowCopy(fileOptions);
		if (fileOptions.ReaderOptions is not null) options.ReaderOptions = _reflectionUtility.GetShallowCopy(fileOptions.ReaderOptions);
		if (fileOptions.ItemType is not null) options.ItemType = fileOptions.ItemType;

		if (options.RepositoryType is null && options.RepositoryTypeName is not null) options.RepositoryType = _reflectionUtility.GetRequiredClassType(options.RepositoryTypeName);
		if (options.ItemType is null && options.ItemTypeName is not null) options.ItemType = _reflectionUtility.GetRequiredClassType(options.ItemTypeName);

		if (options.RepositoryType is null && options.ItemType is not null)
		{
			var repoInterfaceType = _reflectionUtility.TypeMakeGeneric(typeof(ICsvRepository<>), options.ItemType!);
			options.RepositoryType = _reflectionUtility.GetClasses(x => !x.IsGenericType && repoInterfaceType.IsAssignableFrom(x)).FirstOrDefault();
		}
		if (options.RepositoryType is not null && options.ItemType is null)
		{
			var repoInterfaceType = _reflectionUtility.GetGenericInterface(options.RepositoryType, typeof(ICsvRepository<>));
			if (repoInterfaceType is null) throw new InvalidOperationException($"Type {options.RepositoryType.Name} does not implement {typeof(ICsvRepository<>).Name}");
			options.ItemType = repoInterfaceType.GetGenericArguments()[0];
		}

		if (options.ItemType is null) throw new InvalidOperationException($"Can't determine type for file {options.FilePath}");
		if (options.RepositoryType is null) throw new InvalidOperationException($"Can't determine repository type for file {options.FilePath}");

		return options;
	}

	private ICsvUpdateTask CreateUpdateTask(CsvUpdateTaskOptions options)
	{
		Type? taskType = options.TaskTypeName is null ? options.TaskType : _reflectionUtility.GetRequiredClassType(options.TaskTypeName);
		if (taskType is null) throw new InvalidOperationException("Task type not defined");

		ICsvUpdateTask instance = (ICsvUpdateTask)_reflectionUtility.ActivatorCreateInstance(taskType, _serviceProvider);
		instance.IsDisabled = options.IsDisabled;
		instance.IntervalInMinutes = options.IntervalInMinutes;
		instance.RetryIntervalInMinutes = options.RetryIntervalInMinutes;
		instance.AffectedTypes = options.AffectedTypeNames?.Select(_reflectionUtility.GetRequiredClassType).ToArray();
		if (options.Properties is not null) _reflectionUtility.SetValueTypeOrStringProperties(instance, options.Properties);

		return instance;
	}

	private void LoadRepository<T>(string filePath, bool clearRepository, CsvReaderOptions? csvReaderOptions) where T : class, new()
	{
		ICsvRepository<T> repository = _serviceProvider?.GetRequiredService<ICsvRepository<T>>() ?? throw new InvalidOperationException($"IRepository<{typeof(T).Name}> not found");
		if (clearRepository) repository.Clear();
		using var reader = _csvReaderFactory.Create<T>(filePath, csvReaderOptions);
		reader.Error += OnReaderError;
		while (!reader.EndOfStream)
		{
			T[] items = reader.ReadObjects(_readerReadSize);
			OnDataLoaded(typeof(T), items);
			repository.Add(items);
		}
	}

	private async void OnUpdateTimer(object? state)
		=> await UpdateAsync();

	private void OnReaderError(object? sender, UnhandledExceptionEventArgs e)
	{
		if (ReaderError is null) throw (Exception)e.ExceptionObject;
		ReaderError(sender, e);
	}

	private void OnTaskError(IResult errorResult)
	{
		if (UpdateTaskError is null) throw new InvalidOperationException(errorResult.Message, errorResult.SourceException);
		UpdateTaskError(this, new ResultEventArgs(errorResult));
	}

	private void OnDataLoaded(Type type, object[] items)
	{
		if (DataLoaded is null) return;
		DataLoaded(this, new DataLoadedEventArgs(type, items));
	}
}
