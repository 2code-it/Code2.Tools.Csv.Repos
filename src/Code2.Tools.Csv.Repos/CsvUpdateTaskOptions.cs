using System;
using System.Collections.Generic;

namespace Code2.Tools.Csv.Repos;
public class CsvUpdateTaskOptions
{
	public string? TaskTypeName { get; set; }
	public Type? TaskType { get; set; }
	public string[]? AffectedTypeNames { get; set; }
	public int IntervalInMinutes { get; set; }
	public int? RetryIntervalInMinutes { get; set; }
	public bool IsDisabled { get; set; }
	public Dictionary<string, string>? Properties { get; set; }

}
