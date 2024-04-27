using Code2.Tools.Csv.Repos.Internals;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos.Tests
{
	[TestClass]
	public class CsvUpdaterTests
	{
		private CsvReposOptions _options = default!;
		private ICsvLoader _csvLoader = default!;
		private IReflectionUtility _reflectionUtility = default!;


		[TestMethod]
		public void Start_When_Started_Expect_IsRunningTrue()
		{
			ResetDependencies();
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);

			csvUpdater.Start();

			Assert.IsTrue(csvUpdater.IsRunning);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void Start_When_StartedTwice_Expect_Exception()
		{
			ResetDependencies();
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);

			csvUpdater.Start();
			csvUpdater.Start();

			Assert.IsTrue(csvUpdater.IsRunning);
		}

		[TestMethod]
		public void Start_When_StopAfterStart_Expect_IsRunningFalse()
		{
			ResetDependencies();
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);

			csvUpdater.Start();
			csvUpdater.Stop();

			Assert.IsFalse(csvUpdater.IsRunning);
		}

		[TestMethod]
		public async Task RunAllTasksAsync_When_TaskDueTimeAndCanRunTrue_Expect_TaskRun()
		{
			ResetDependencies();
			ICsvUpdateTask updateTask = Substitute.For<ICsvUpdateTask>();
			updateTask.LastRun.Returns(DateTime.Now.AddHours(-2));
			updateTask.IntervalInHours.Returns(1);
			updateTask.CanRunAsync().Returns(Task.FromResult(true));
			updateTask.RunAsync().Returns(Task.CompletedTask);
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);
			csvUpdater.Tasks.Add(updateTask);

			await csvUpdater.RunAllTasksAsync();

			await updateTask.Received(1).RunAsync();
		}

		[TestMethod]
		public async Task RunAllTasksAsync_When_TaskDueTimeAndCanRunFalse_Expect_TaskNotRun()
		{
			ResetDependencies();
			ICsvUpdateTask updateTask = Substitute.For<ICsvUpdateTask>();
			updateTask.LastRun.Returns(DateTime.Now.AddHours(-2));
			updateTask.IntervalInHours.Returns(1);
			updateTask.CanRunAsync().Returns(Task.FromResult(false));
			updateTask.RunAsync().Returns(Task.CompletedTask);
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);
			csvUpdater.Tasks.Add(updateTask);

			await csvUpdater.RunAllTasksAsync();

			await updateTask.Received(0).RunAsync();
		}

		[TestMethod]
		public async Task RunAllTasksAsync_When_NotTaskDueTimeAndCanRun_Expect_TaskNotRun()
		{
			ResetDependencies();
			ICsvUpdateTask updateTask = Substitute.For<ICsvUpdateTask>();
			updateTask.LastRun.Returns(DateTime.Now);
			updateTask.IntervalInHours.Returns(1);
			updateTask.CanRunAsync().Returns(Task.FromResult(true));
			updateTask.RunAsync().Returns(Task.CompletedTask);
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);
			csvUpdater.Tasks.Add(updateTask);

			await csvUpdater.RunAllTasksAsync();

			await updateTask.Received(0).RunAsync();
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public async Task RunAllTasksAsync_When_TaskRunThrows_Expect_Exception()
		{
			ResetDependencies();
			ICsvUpdateTask updateTask = Substitute.For<ICsvUpdateTask>();
			updateTask.LastRun.Returns(DateTime.Now.AddHours(-1));
			updateTask.IntervalInHours.Returns(1);
			updateTask.CanRunAsync().Returns(Task.FromResult(true));
			updateTask.RunAsync().ThrowsAsync(new InvalidOperationException("task failed"));
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);
			csvUpdater.Tasks.Add(updateTask);

			await csvUpdater.RunAllTasksAsync();
		}

		[TestMethod]
		public async Task RunAllTasksAsync_When_TaskRunThrowsAndTaskErrorEventSet_Expect_NoException()
		{
			ResetDependencies();
			ICsvUpdateTask updateTask = Substitute.For<ICsvUpdateTask>();
			updateTask.LastRun.Returns(DateTime.Now.AddHours(-1));
			updateTask.IntervalInHours.Returns(1);
			updateTask.CanRunAsync().Returns(Task.FromResult(true));
			updateTask.RunAsync().ThrowsAsync(new InvalidOperationException("task failed"));
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);
			csvUpdater.TaskError += (s, e) => { };
			csvUpdater.Tasks.Add(updateTask);

			await csvUpdater.RunAllTasksAsync();
		}

		[TestMethod]
		public async Task RunAllTasksAsync_When_TaskTargetTypeNamesSet_Expect_CsvLoaderLoad()
		{
			ResetDependencies();
			ICsvUpdateTask updateTask = Substitute.For<ICsvUpdateTask>();
			updateTask.LastRun.Returns(DateTime.Now.AddHours(-1));
			updateTask.IntervalInHours.Returns(1);
			updateTask.ReloadTargetTypeNames = new[] { "*" };
			updateTask.CanRunAsync().Returns(Task.FromResult(true));
			updateTask.RunAsync().Returns(Task.CompletedTask);
			using CsvUpdater csvUpdater = new CsvUpdater(_options, _csvLoader, _reflectionUtility);
			csvUpdater.Tasks.Add(updateTask);

			await csvUpdater.RunAllTasksAsync();

			await _csvLoader.Received(1).LoadAsync(Arg.Any<string[]>());
		}

		private void ResetDependencies()
		{
			_options = new CsvReposOptions();
			_reflectionUtility = Substitute.For<IReflectionUtility>();
			_csvLoader = Substitute.For<ICsvLoader>();
		}
	}
}