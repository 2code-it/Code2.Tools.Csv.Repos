using System;

namespace Code2.Tools.Csv.Repos;
public class DataLoadedEventArgs : EventArgs
{
	public DataLoadedEventArgs(Type type, object[] data)
	{
		Type = type;
		Data = data;
	}

	public Type Type { get; private set; }
	public object[] Data { get; private set; }
}
