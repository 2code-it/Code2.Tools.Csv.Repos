using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos;
public interface ICsvReposManager
{
	Action<IResult>? UpdateTaskError { get; set; }
	void Configure(CsvReposOptions options, IServiceProvider? serviceProvider = null);
	void Configure(Action<CsvReposOptions> config, IServiceProvider? serviceProvider = null);
	Task LoadAsync(Type[]? targetItemTypes = null, CancellationToken cancellationToken = default);
	Task UpdateAsync(CancellationToken cancellationToken = default);
}