using Code2.Tools.Csv.Repos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Tools.Csv.ReposTests
{
	public class TestRepository<TestItem> : IRepository<TestItem>
	{
		public void Add(IEnumerable<TestItem> items)
		{
		}

		public void Clear()
		{
		}

		public IEnumerable<TestItem> Get(Func<TestItem, bool> filter)
		{
			return Enumerable.Empty<TestItem>();
		}
	}
}
