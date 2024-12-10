using System;

namespace Code2.Tools.Csv.Repos;

public class CsvFileInfo
{
	public CsvFileInfo(string fullFilePath, object repository, Type itemType, CsvReaderOptions? csvReaderOptions = null)
	{
		FullFilePath = fullFilePath;
		Repository = repository;
		ItemType = itemType;
		CsvReaderOptions = csvReaderOptions;
	}

	public string FullFilePath { get; private set; }
	public object Repository { get; private set; }
	public Type ItemType { get; private set; }
	public CsvReaderOptions? CsvReaderOptions { get; private set; }
}