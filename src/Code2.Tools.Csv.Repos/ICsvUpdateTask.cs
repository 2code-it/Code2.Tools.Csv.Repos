using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos;
public interface ICsvUpdateTask
{
	public int IntervalInMinutes { get; set; }
	public int? RetryIntervalInMinutes { get; set; }
	public DateTime RunAfter { get; set; }
	public bool IsRunning { get; }
	public bool IsDisabled { get; set; }
	public Type[]? AffectedTypes { get; set; }

	public Task<IResult> RunAsync(CancellationToken cancellationToken = default);
}
