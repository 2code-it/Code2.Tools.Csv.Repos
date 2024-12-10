using System;
using System.Collections.Generic;
using System.Text;

namespace Code2.Tools.Csv.Repos;
public static class OptionsExtension
{
	public static CsvReposOptions AddFile<T>(this CsvReposOptions options, string filePath, Action<CsvReaderOptions>? configReaderOptions = null)
		where T : class
	{
		Type type = typeof(T);
		CsvReaderOptions? readerOptions = GetReaderOptions(configReaderOptions);
		AddFileInternal(options, filePath, type, null, null, readerOptions);
		return options;
	}

	private static void AddFileInternal(CsvReposOptions options, string filePath, Type? type = null, string? typeName = null, object? repo = null, CsvReaderOptions? readerOptions = null)
	{
		List<CsvFileOptions> list = new List<CsvFileOptions>();
		if (options.Files is not null) list.AddRange(options.Files);

		CsvFileOptions fileOptions = new CsvFileOptions
		{
			FilePath = filePath,
			Type = type,
			TypeName = typeName,
			Repository = repo,
			ReaderOptions = readerOptions
		};

		list.Add(fileOptions);
		options.Files = list.ToArray();
	}

	private static CsvReaderOptions? GetReaderOptions(Action<CsvReaderOptions>? configReaderOptions)
	{
		if(configReaderOptions is null) return null;
		CsvReaderOptions csvReaderOptions = new CsvReaderOptions();
		configReaderOptions?.Invoke(csvReaderOptions);
		return csvReaderOptions;
	}
}
