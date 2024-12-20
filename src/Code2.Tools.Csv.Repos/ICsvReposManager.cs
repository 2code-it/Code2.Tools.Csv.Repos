using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos;
public interface ICsvReposManager
{
	event EventHandler<ResultEventArgs>? UpdateTaskError;
	event EventHandler<UnhandledExceptionEventArgs>? ReaderError;
	event EventHandler<DataLoadedEventArgs>? DataLoaded;
	void Configure(CsvReposOptions options);
	void Configure(Action<CsvReposOptions> config);
	Task LoadAsync(Type[]? targetItemTypes = null, CancellationToken cancellationToken = default);
	Task UpdateAsync(CancellationToken cancellationToken = default);
}