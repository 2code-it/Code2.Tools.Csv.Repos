using System;

namespace Code2.Tools.Csv.Repos;

public interface ICsvFileInfo
{
	string FilePath { get; }
	Type? ItemType { get; }
	Type? RepositoryType { get; }
	CsvReaderOptions? ReaderOptions { get; }
	bool IsTransientRepository { get; }
}