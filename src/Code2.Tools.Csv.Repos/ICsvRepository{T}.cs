using System;
using System.Collections.Generic;

namespace Code2.Tools.Csv.Repos;

public interface ICsvRepository<T>
{
	IEnumerable<T> Get(Func<T, bool>? filter = null);
	void Add(IEnumerable<T> items);
	void Clear();
}
