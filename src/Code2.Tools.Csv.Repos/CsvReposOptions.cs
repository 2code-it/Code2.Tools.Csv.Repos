using System.Collections.Generic;

namespace Code2.Tools.Csv.Repos
{
	public class CsvReposOptions
	{
		public string CsvDataDirectory { get; set; } = "./data";
		public List<CsvFileInfo> Files { get; set; } = new List<CsvFileInfo>();
		public List<CsvUpdateTaskInfo> UpdateTasks { get; set; } = new List<CsvUpdateTaskInfo>();
		public CsvReaderOptions CsvReaderOptions { get; set; } = new CsvReaderOptions() { IgnoreEmptyWhenDeserializing = true };
		public int CsvReaderReadAmount { get; set; } = 5000;
		public bool LoadOnStart { get; set; }
		public bool UpdateOnStart { get; set; }
	}
}
