namespace Code2.Tools.Csv.Repos
{
	public class CsvFileInfo
	{
		public string NameFilter { get; set; } = string.Empty;
		public string TargetTypeName { get; set; } = string.Empty;
		public string RepositoryTypeName { get; set; } = "MemoryRepository`1";
		public CsvReaderOptions? ReaderOptions { get; set; }
	}
}
