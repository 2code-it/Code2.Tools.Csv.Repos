using System;

namespace Code2.Tools.Csv.Repos;
public class ResultEventArgs : EventArgs
{
	public ResultEventArgs(IResult result)
	{
		Result = result;
	}

	public IResult Result { get; private set; }
}
