using Code2.Tools.Csv.Repos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Tools.Csv.ReposTests.Assets
{
	public class TestRepositoryGeneric<T> : ICsvRepository<T>
	{
		public void Add(IEnumerable<T> items)
		{
		}

		public void Clear()
		{
		}

		public IEnumerable<T> Get(Func<T, bool>? filter = null)
		{
			return Enumerable.Empty<T>();
		}
	}
}
