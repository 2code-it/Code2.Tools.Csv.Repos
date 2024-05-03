using Code2.Tools.Csv.Repos.Internals;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos
{
	public class CsvLoader : ICsvLoader
	{
		public CsvLoader(IServiceProvider serviceProvider) :
			this(serviceProvider, serviceProvider.GetRequiredService<ICsvReaderFactory>(), serviceProvider.GetRequiredService<CsvReposOptions>())
		{ }
		public CsvLoader(IServiceProvider serviceProvider, ICsvReaderFactory csvReaderFactory, CsvReposOptions options) :
			this(serviceProvider, csvReaderFactory, options, new ReflectionUtility(), new FileSystem())
		{ }
		internal CsvLoader(IServiceProvider serviceProvider, ICsvReaderFactory csvReaderFactory, CsvReposOptions options, IReflectionUtility reflectionUtility, IFileSystem fileSystem)
		{
			_serviceProvider = serviceProvider;
			_csvReaderFactory = csvReaderFactory;
			_options = options;
			_reflectionUtility = reflectionUtility;
			_fileSystem = fileSystem;
		}

		private readonly IServiceProvider _serviceProvider;
		private readonly ICsvReaderFactory _csvReaderFactory;
		private readonly CsvReposOptions _options;
		private readonly IReflectionUtility _reflectionUtility;
		private readonly IFileSystem _fileSystem;

		public event EventHandler<UnhandledExceptionEventArgs>? CsvReaderError;

		public async Task LoadAsync(string[]? targetTypes = null)
		{
			var fileInfos = _options.Files.Where(x => targetTypes is null || targetTypes.Contains(x.TargetTypeName)).OrderBy(x => x.TargetTypeName).ToArray();
			string previousTargetType = string.Empty;
			foreach (CsvFileInfo fileInfo in fileInfos)
			{
				await LoadFileAsync(null, fileInfo, previousTargetType != fileInfo.TargetTypeName);
				previousTargetType = fileInfo.TargetTypeName;
			}
		}

		public async Task LoadFileAsync(string fileNameOrFilter, bool clearRepository = true)
		{
			await LoadFileAsync(fileNameOrFilter, null, clearRepository);
		}

		public async Task LoadFileAsync(CsvFileInfo fileInfo, bool clearRepository = true)
		{
			await LoadFileAsync(null, fileInfo, clearRepository);
		}

		private async Task LoadFileAsync(string? fileNameOrFilter = null, CsvFileInfo? fileInfo = null, bool clearRepository = true)
		{
			EnsureDataDirectoryExists();
			if (fileNameOrFilter is null && fileInfo is null) throw new NotSupportedException();
			if (fileInfo is null)
			{
				fileInfo = _options.Files.FirstOrDefault(x => fileNameOrFilter!.ToLower().Contains(x.NameFilter.ToLower())) ?? throw new InvalidOperationException($"File options not defined for '{fileNameOrFilter}'");
			}

			string? filePath = GetFilePathByFilter(fileInfo);
			if (filePath is null) throw new InvalidOperationException($"File path not found for filter {fileInfo.NameFilter}");

			Type interfaceType = _reflectionUtility.GetRepositoryInterfaceType(fileInfo.RepositoryTypeName, fileInfo.TargetTypeName);
			Type targetType = _reflectionUtility.GetRequiredType(fileInfo.TargetTypeName);
			object? repo = _serviceProvider.GetService(interfaceType);
			CsvReaderOptions readerOptions = fileInfo.ReaderOptions ?? _options.CsvReaderOptions;

			if (repo is null) throw new InvalidOperationException($"{nameof(IServiceProvider)} does not contain {interfaceType}");
			await Task.Run(() => _reflectionUtility.InvokePrivateGenericMethod(this, nameof(LoadFileAsAsync), targetType, new[] { filePath, repo, readerOptions, clearRepository }));
		}

		public virtual void OnLoadData<T>(T[] items) where T : notnull { }
		public virtual void OnCsvReaderError(Exception exception, ref bool handled) { }

		protected void LoadFileAsAsync<T>(string filePath, object repository, CsvReaderOptions readerOptions, bool clearRepository = true) where T : class, new()
		{
			IRepository<T> repo = (IRepository<T>)repository;
			if (clearRepository) repo.Clear();
			using ICsvReader<T> csvReader = _csvReaderFactory.Create<T>(filePath, readerOptions);
			csvReader.Error += CsvReaderErrorHandler;
			while (!csvReader.EndOfStream)
			{
				T[] data = csvReader.ReadObjects(_options.CsvReaderReadAmount);
				OnLoadData(data);
				repo.Add(data);
			}
		}

		private void CsvReaderErrorHandler(object sender, UnhandledExceptionEventArgs eventArgs)
		{
			bool handled = CsvReaderError is not null;
			OnCsvReaderError((Exception)eventArgs.ExceptionObject, ref handled);
			CsvReaderError?.Invoke(this, eventArgs);
			if (!handled) throw (Exception)eventArgs.ExceptionObject;
		}

		private void EnsureDataDirectoryExists()
		{
			string dataFullPath = _fileSystem.PathGetFullPath(_options.CsvDataDirectory);
			_fileSystem.DirectoryCreate(dataFullPath);
		}

		private string? GetFilePathByFilter(CsvFileInfo fileInfo)
		{
			string dataFullPath = _fileSystem.PathGetFullPath(_options.CsvDataDirectory);
			string[] filePaths = _fileSystem.DirectoryGetFiles(dataFullPath);
			string? filePath = filePaths.FirstOrDefault(x => x.ToLower().Contains(fileInfo.NameFilter.ToLower()));
			return filePath;
		}
	}
}
