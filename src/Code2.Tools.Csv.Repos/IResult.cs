using System;

namespace Code2.Tools.Csv.Repos;
public interface IResult
{
	public ResultState State { get; }
	public bool IsSuccess { get; }
	public string? Message { get; }
	public Exception? SourceException { get; }
}
