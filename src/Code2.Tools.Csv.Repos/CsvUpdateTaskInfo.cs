using System.Collections.Generic;

namespace Code2.Tools.Csv.Repos
{
	public class CsvUpdateTaskInfo
	{
		public int IntervalInHours { get; set; }
		public string TaskTypeName { get; set; } = string.Empty;
		public Dictionary<string, string> TaskProperties { get; set; } = new Dictionary<string, string>();
		public string[]? ReloadTargetTypeNames { get; set; }
	}
}
