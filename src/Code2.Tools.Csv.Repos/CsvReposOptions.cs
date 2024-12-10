﻿namespace Code2.Tools.Csv.Repos;
public class CsvReposOptions
{
	public CsvReaderOptions? DefaultReaderOptions { get; set; }
	public int? CsvReadSize { get; set; }
	public CsvFileOptions[]? Files { get; set; }
	public CsvUpdateTaskOptions[]? UpdateTasks { get; set; }
	public int? UpdateIntervalInMinutes { get; set; }
	public int? RetryIntervalInMinutes { get; set; }
}
