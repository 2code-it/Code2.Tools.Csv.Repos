using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Tools.Csv.Repos
{
	public class MemoryRepository<T> : IRepository<T>
	{
		private readonly List<T> _list = new List<T>();
		private readonly object _lock = new object();

		public virtual void Add(IEnumerable<T> items)
		{
			lock (_lock)
			{
				_list.AddRange(items);
			}
		}

		public virtual void Clear()
		{
			lock (_lock)
			{
				_list.Clear();
			}
		}

		public virtual IEnumerable<T> Get(Func<T, bool> filter)
		{
			lock (_lock)
			{
				return _list.Where(filter);
			}
		}
	}
}
