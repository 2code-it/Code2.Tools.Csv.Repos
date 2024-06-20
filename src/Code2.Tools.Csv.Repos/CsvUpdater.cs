using Code2.Tools.Csv.Repos.Internals;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos
{
	public class CsvUpdater : ICsvUpdater, IDisposable
	{
		public CsvUpdater(ICsvLoader csvLoader, CsvReposOptions? options) : this(csvLoader, options, new ReflectionUtility()) { }
		internal CsvUpdater(ICsvLoader csvLoader, CsvReposOptions? options, IReflectionUtility reflectionUtility)
		{
			_csvLoader = csvLoader;
			_reflectionUtility = reflectionUtility;
			if (options is not null) Configure(options);
		}

		private readonly ICsvLoader _csvLoader;
		private readonly IReflectionUtility _reflectionUtility;
		private const int _ms_per_hour = 3600000;
		private Timer? _timer;

		public event EventHandler<UnhandledExceptionEventArgs>? TaskError;
		public bool IsRunning => _timer is not null;
		public List<ICsvUpdateTask> Tasks { get; private set; } = new List<ICsvUpdateTask>();

		public void Start()
		{
			if (_timer is not null) throw new InvalidOperationException($"{nameof(CsvUpdater)} already started");
			_timer = new Timer(new TimerCallback(OnTimerTick), null, GetNextFullHourOffsetInMs(), _ms_per_hour);
		}

		public void Stop()
		{
			_timer?.Dispose();
			_timer = null;
		}

		protected virtual void OnBeforeTaskRun(ICsvUpdateTask updateTask) { }
		protected virtual void OnAfterTaskRun(ICsvUpdateTask updateTask) { }
		protected virtual void OnTaskError(ICsvUpdateTask updateTask, Exception exception, ref bool handled) { }

		public async Task RunAllTasksAsync()
		{
			await RunAllTasksInner();
		}

		public void Configure(CsvReposOptions options)
		{
			Tasks.Clear();
			Tasks.AddRange(GetTasksFromOptions(options));
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}

		private async void OnTimerTick(object? state)
		{
			await RunAllTasksInner();
		}

		private async Task RunAllTasksInner()
		{
			DateTime now = DateTime.Now;
			var tasksToRun = Tasks.Where(x => (now - x.LastRun).TotalHours >= x.IntervalInHours);
			foreach (var updateTask in tasksToRun)
			{
				try
				{
					if (!await updateTask.CanRunAsync()) continue;
					OnBeforeTaskRun(updateTask);
					await updateTask.RunAsync();
					updateTask.LastRun = now;
					OnAfterTaskRun(updateTask);
				}
				catch (Exception ex)
				{
					OnTaskErrorInner(updateTask, ex);
					continue;
				}

				if ((updateTask.ReloadTargetTypeNames?.Length ?? 0) == 0) continue;
				await _csvLoader.LoadAsync(updateTask.ReloadTargetTypeNames![0] == "*" ? null : updateTask.ReloadTargetTypeNames);
			}
		}
		private void OnTaskErrorInner(ICsvUpdateTask updateTask, Exception exception)
		{
			bool handled = TaskError is not null;
			string exceptionMessage = $"Update task '{updateTask.GetType().Name}' failed";
			InvalidOperationException invalidOperationException = new InvalidOperationException(exceptionMessage, exception);
			OnTaskError(updateTask, invalidOperationException, ref handled);
			TaskError?.Invoke(this, new UnhandledExceptionEventArgs(invalidOperationException, false));
			if (!handled) throw invalidOperationException;
		}

		private ICsvUpdateTask[] GetTasksFromOptions(CsvReposOptions options)
			=> options.UpdateTasks.Select(x => GetTaskFromUpdateTaskInfo(x, options)).ToArray();

		private ICsvUpdateTask GetTaskFromUpdateTaskInfo(CsvUpdateTaskInfo taskInfo, CsvReposOptions reposOptions)
		{
			Type taskType = _reflectionUtility.GetRequiredType(taskInfo.TaskTypeName);
			object?[]? constructorParams = _reflectionUtility.HasConstructorFor(taskType, new[] { typeof(CsvReposOptions) }) ? new[] { reposOptions } : null;
			ICsvUpdateTask taskInstance = (ICsvUpdateTask?)Activator.CreateInstance(taskType, constructorParams) ?? throw new InvalidOperationException($"Can't create instance of '{taskInfo.TaskTypeName}'");
			_reflectionUtility.SetProperties(taskInstance, taskInfo.TaskProperties);
			taskInstance.IntervalInHours = taskInfo.IntervalInHours;
			taskInstance.ReloadTargetTypeNames = taskInfo.ReloadTargetTypeNames;
			return taskInstance;
		}

		private static int GetNextFullHourOffsetInMs()
			=> _ms_per_hour - (int)Math.Round(DateTime.Now.TimeOfDay.TotalMilliseconds) % _ms_per_hour;
	}
}
