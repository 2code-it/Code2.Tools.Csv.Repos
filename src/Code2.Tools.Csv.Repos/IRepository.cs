using System;
using System.Collections.Generic;

namespace Code2.Tools.Csv.Repos
{
	public interface IRepository<T>
	{
		IEnumerable<T> Get(Func<T, bool> filter);
		void Add(IEnumerable<T> items);
		void Clear();
	}
}
