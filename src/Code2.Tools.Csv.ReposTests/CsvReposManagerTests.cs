using Code2.Tools.Csv.Repos;
using Code2.Tools.Csv.Repos.Internals;
using Code2.Tools.Csv.ReposTests.Assets;
using NSubstitute;
using System;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.ReposTests;

[TestClass]
public class CsvReposManagerTests
{

	private IReflectionUtility _reflectionUtility = default!;
	private ICsvReaderFactory _csvReaderFactory = default!;
	private IFileSystem _fileSystem = default!;

	[TestInitialize]
	public void TestInitialize()
	{
		_reflectionUtility = Substitute.For<IReflectionUtility>();
		_csvReaderFactory = Substitute.For<ICsvReaderFactory>();
		_fileSystem = Substitute.For<IFileSystem>();
	}

	[TestMethod]
	public void Configure_When_FileOptionsSet_Expect_CorrespondingFileCount()
	{
		var reposManager = new CsvReposManager(_csvReaderFactory, new ReflectionUtility(), _fileSystem);
		var options = new CsvReposOptions { Files = new[] { new CsvFileOptions { TypeName = nameof(TestItem) } } };

		reposManager.Configure(options);

		Assert.AreEqual(1, reposManager.Files.Length);
	}

	[TestMethod]
	public void Configure_When_FileOptionsSet_Expect_CorrespondingFileInfo()
	{
		string expectedFullFilePath = "/test.csv";
		Type expectedItemType = typeof(TestItem);
		var reposManager = new CsvReposManager(_csvReaderFactory, new ReflectionUtility(), _fileSystem);
		CsvFileOptions fileOptions = new();
		_fileSystem.PathGetFullPath(Arg.Any<string>()).Returns(expectedFullFilePath);
		fileOptions.FilePath = "./test.csv";
		fileOptions.ReaderOptions = new CsvReaderOptions { Explicit = true };
		fileOptions.TypeName = nameof(TestItem);
		var options = new CsvReposOptions { Files = new[] { fileOptions } };
		reposManager.Configure(options);

		CsvFileInfo fileInfo = reposManager.Files[0];

		Assert.AreEqual(typeof(TestRepository).Name, fileInfo.Repository.GetType().Name);
		Assert.AreEqual(fileOptions.ReaderOptions.Explicit, fileInfo.CsvReaderOptions!.Explicit);
		Assert.AreEqual(expectedFullFilePath, fileInfo.FullFilePath);
		Assert.AreEqual(expectedItemType, fileInfo.ItemType);
	}

	[TestMethod]
	public void Configure_When_UpdateTasksOptionsSet_Expect_CorrespondingUpdateTaskCount()
	{
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestUpdateTask));
		_reflectionUtility.ActivatorCreateInstance(Arg.Any<Type>(), Arg.Any<IServiceProvider>()).Returns(new TestUpdateTask());
		var options = new CsvReposOptions { UpdateTasks = new[] { new CsvUpdateTaskOptions { TaskTypeName = nameof(TestUpdateTask) } } };

		reposManager.Configure(options);

		Assert.AreEqual(1, reposManager.UpdateTasks.Length);
	}

	[TestMethod]
	public void Configure_When_UpdateTasksOptionsSet_Expect_CorrespondingUpdateTask()
	{
		var reposManager = new CsvReposManager(_csvReaderFactory, new ReflectionUtility(), _fileSystem);
		CsvUpdateTaskOptions taskOptions = new();
		taskOptions.TaskTypeName = nameof(TestUpdateTask);
		taskOptions.AffectedTypeNames = new[] { nameof(TestItem) };
		taskOptions.IntervalInMinutes = 25;
		taskOptions.IsDisabled = true;
		taskOptions.Properties = new() { { "ThrowsError", "false" }, { "FilePath", "./test.csv" } };
		var options = new CsvReposOptions { UpdateTasks = new[] { taskOptions } };
		reposManager.Configure(options);

		TestUpdateTask updateTask = (TestUpdateTask)reposManager.UpdateTasks[0];

		Assert.AreEqual(typeof(TestItem), updateTask.AffectedTypes![0]);
		Assert.AreEqual(taskOptions.IsDisabled, updateTask.IsDisabled);
		Assert.AreEqual(taskOptions.IntervalInMinutes, updateTask.IntervalInMinutes);
		Assert.AreEqual(taskOptions.Properties["FilePath"], updateTask.FilePath);
		Assert.AreEqual(Convert.ToBoolean(taskOptions.Properties["ThrowsError"]), updateTask.ThrowsError);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public async Task UpdateAsync_When_UpdateTaskStateIsErrorAndUpdateTaskErrorNotSet_Expect_Exception()
	{
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestUpdateTask));
		_reflectionUtility.ActivatorCreateInstance(Arg.Any<Type>(), Arg.Any<IServiceProvider>()).Returns(new TestUpdateTask(true));
		var options = new CsvReposOptions { UpdateTasks = new[] { new CsvUpdateTaskOptions { TaskTypeName = "TestUpdateTask" } } };

		reposManager.Configure(options);
		await reposManager.UpdateAsync();
	}

	[TestMethod]
	public async Task UpdateAsync_When_UpdateTaskStateIsErrorAndUpdateTaskErrorSet_Expect_ExceptionHandled()
	{
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		reposManager.UpdateTaskError = (e) => { };
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestUpdateTask));
		_reflectionUtility.ActivatorCreateInstance(Arg.Any<Type>(), Arg.Any<IServiceProvider>()).Returns(new TestUpdateTask(true));
		var options = new CsvReposOptions { UpdateTasks = new[] { new CsvUpdateTaskOptions { TaskTypeName = "TestUpdateTask" } } };

		reposManager.Configure(options);
		await reposManager.UpdateAsync();
	}

	[TestMethod]
	public async Task UpdateAsync_When_UpdateTaskResultIsNotSuccess_Expect_TaskRunAfterIncrementByRetryInterval()
	{
		int retryInterval = 10;
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		reposManager.UpdateTaskError = (e) => { };
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestUpdateTask));
		_reflectionUtility.ActivatorCreateInstance(Arg.Any<Type>(), Arg.Any<IServiceProvider>()).Returns(new TestUpdateTask(true));
		var options = new CsvReposOptions { RetryIntervalInMinutes = retryInterval, UpdateTasks = new[] { new CsvUpdateTaskOptions { TaskTypeName = "TestUpdateTask" } } };
		var minRetryTime = DateTime.Now.AddMinutes(retryInterval);

		reposManager.Configure(options);
		await reposManager.UpdateAsync();

		Assert.IsTrue(reposManager.UpdateTasks[0].RunAfter.Minute == minRetryTime.Minute);
	}
}
