using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Tools.Csv.Repos;
public static class OptionsExtension
{
	public static CsvReposOptions AddFile<T>(this CsvReposOptions options, string filePath, Action<CsvReaderOptions>? configReaderOptions = null)
		where T : class
	{
		CsvReaderOptions? readerOptions = GetReaderOptions(configReaderOptions);

		var list = options.Files?.ToList() ?? new List<CsvFileOptions>();
		list.Add(new CsvFileOptions { FilePath = filePath, ItemType = typeof(T), ReaderOptions = readerOptions });

		return options;
	}

	private static CsvReaderOptions? GetReaderOptions(Action<CsvReaderOptions>? configReaderOptions)
	{
		if (configReaderOptions is null) return null;
		CsvReaderOptions csvReaderOptions = new();
		configReaderOptions?.Invoke(csvReaderOptions);
		return csvReaderOptions;
	}
}
