using Microsoft.Extensions.DependencyInjection;
using System;

namespace Code2.Tools.Csv.Repos;
public class CsvReposOptions
{
	public CsvReaderOptions? DefaultReaderOptions { get; set; }
	public int? ReaderReadSize { get; set; }
	public CsvFileOptions[]? Files { get; set; }
	public CsvUpdateTaskOptions[]? UpdateTasks { get; set; }
	public int? UpdateIntervalInMinutes { get; set; }
	public int? RetryIntervalInMinutes { get; set; }
	public IServiceCollection? ServiceCollection { get; set; }
	public IServiceProvider? ServiceProvider { get; set; }
	public Action<DataLoadedEventArgs>? OnDataLoaded { get; set; }
	public Action<UnhandledExceptionEventArgs>? OnReaderError { get; set; }
	public Action<ResultEventArgs>? OnUpdateTaskError { get; set; }
}
