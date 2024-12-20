using Code2.Tools.Csv.Repos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.ReposTests.Assets;
public class TestUpdateTask : ICsvUpdateTask
{
	public TestUpdateTask() { }

	public int IntervalInMinutes { get; set; }
	public int? RetryIntervalInMinutes { get; set; }
	public DateTime RunAfter { get; set; }
	public bool IsRunning { get; private set; }
	public bool IsDisabled { get; set; }
	public Type[]? AffectedTypes { get; set; }

	public string? FilePath { get; set; }
	public bool ThrowsError { get; set; }
	public ResultState ResultState { get; set; }
	public string? ResultMessage { get; set; }

	public Task<IResult> RunAsync(CancellationToken cancellationToken = default)
	{
		if (ThrowsError) throw new InvalidOperationException("Error");
		return Task.FromResult(Result.Create(ResultState, ResultMessage));
	}
}
