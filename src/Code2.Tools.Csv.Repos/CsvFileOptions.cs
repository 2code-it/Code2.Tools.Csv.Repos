using System;

namespace Code2.Tools.Csv.Repos;
public class CsvFileOptions
{
	public string FilePath { get; set; } = string.Empty;
	public string? TypeName { get; set; }
	public Type? Type { get; set; }
	public object? Repository { get; set; }
	public CsvReaderOptions? ReaderOptions { get; set; }
}
