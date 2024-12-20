using System;

namespace Code2.Tools.Csv.Repos;
public class CsvFileOptions : ICsvFileInfo
{
	public string FilePath { get; set; } = string.Empty;
	public string? ItemTypeName { get; set; }
	public Type? ItemType { get; set; }
	public string? RepositoryTypeName { get; set; }
	public Type? RepositoryType { get; set; }
	public CsvReaderOptions? ReaderOptions { get; set; }
	public bool IsTransientRepository { get; set; }
}
