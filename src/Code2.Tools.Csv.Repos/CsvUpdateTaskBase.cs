using System;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos
{
	public abstract class CsvUpdateTaskBase : ICsvUpdateTask
	{
		public int IntervalInHours { get; set; }
		public DateTime LastRun { get; set; }
		public string[]? ReloadTargetTypeNames { get; set; }

		public abstract Task<bool> CanRunAsync();
		public abstract Task RunAsync();
	}
}
