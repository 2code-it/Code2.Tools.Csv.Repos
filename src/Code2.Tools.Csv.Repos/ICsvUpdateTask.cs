using System;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos
{
	public interface ICsvUpdateTask
	{
		int IntervalInHours { get; set; }
		DateTime LastRun { get; set; }
		string[]? ReloadTargetTypeNames { get; set; }
		Task<bool> CanRunAsync();
		Task RunAsync();
	}
}
