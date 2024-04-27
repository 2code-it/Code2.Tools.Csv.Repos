using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos
{
	public interface ICsvUpdater
	{
		event EventHandler<UnhandledExceptionEventArgs>? TaskError;
		List<ICsvUpdateTask> Tasks { get; }
		bool IsRunning { get; }
		Task RunAllTasksAsync();
		void Start();
		void Stop();
	}
}