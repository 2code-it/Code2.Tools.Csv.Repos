using System;

namespace Code2.Tools.Csv.Repos;

public class Result : IResult
{
	public Result(ResultState state, string? message = null, Exception? sourceException = null)
	{
		State = state;
		Message = message;
		SourceException = sourceException;
	}

	public ResultState State { get; private set; }
	public string? Message { get; private set; }
	public Exception? SourceException { get; private set; }
	public bool IsSuccess => State == ResultState.Success;


	public static IResult Create(ResultState state, string? message = null, Exception? sourceException = null) 
		=> new Result(state, message, sourceException);
	public static IResult Success() 
		=> Create(ResultState.Success);
	public static IResult Cancel(string? message = null) 
		=> Create(ResultState.Cancelled, message);
	public static IResult Error(string? message = null, Exception? sourceException = null) 
		=> Create(ResultState.Error, message, sourceException);
}