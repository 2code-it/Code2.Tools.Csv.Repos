﻿using Code2.Tools.Csv.Repos;
using Code2.Tools.Csv.Repos.Internals;
using Code2.Tools.Csv.ReposTests.Assets;
using Microsoft.Extensions.DependencyInjection;
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
	public void Configure_When_FileOptionsSet_Expect_ResultWithMandatoryPropertiesSet()
	{
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		Type repoType = typeof(TestRepository);
		Type repoInterfaceType = typeof(ICsvRepository<TestItem>);
		_reflectionUtility.GetShallowCopy(Arg.Any<CsvFileOptions>()).Returns(x => x.Arg<CsvFileOptions>());
		_reflectionUtility.TypeMakeGeneric(typeof(ICsvRepository<>), typeof(TestItem)).Returns(x => repoInterfaceType);
		_reflectionUtility.GetClasses(Arg.Any<Func<Type, bool>>()).Returns(new[] { repoType });
		var options = new CsvReposOptions { Files = new[] { new CsvFileOptions { ItemType = typeof(TestItem) } } };

		reposManager.Configure(options);

		_reflectionUtility.Received(1).TypeMakeGeneric(typeof(ICsvRepository<>), typeof(TestItem));
		Assert.AreEqual(1, reposManager.Files.Length);
		Assert.AreEqual(repoType, reposManager.Files[0].RepositoryType);
	}

	[TestMethod]
	public void Configure_When_FileOptionsSetWithServiceCollection_Expect_RepositoriesAddedToCollectionCorrectly()
	{
		IServiceCollection services = Substitute.For<IServiceCollection>();
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		Type repoType = typeof(TestRepository);
		Type repoInterfaceType = typeof(ICsvRepository<TestItem>);
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestItem));
		_reflectionUtility.GetShallowCopy(Arg.Any<CsvFileOptions>()).Returns(x => x.Arg<CsvFileOptions>());
		_reflectionUtility.TypeMakeGeneric(typeof(ICsvRepository<>), typeof(TestItem)).Returns(x => repoInterfaceType);
		_reflectionUtility.GetClasses(Arg.Any<Func<Type, bool>>()).Returns(new[] { repoType });
		var options = new CsvReposOptions 
		{ 
			ServiceCollection = services,
			Files = new[] { new CsvFileOptions { ItemTypeName = nameof(TestItem), IsTransientRepository = true } } 
		};
		ServiceDescriptor? serviceDescriptor = null;
		services.When(x => x.Add(Arg.Any<ServiceDescriptor>())).Do(x => serviceDescriptor = x.Arg<ServiceDescriptor>());

		reposManager.Configure(options);

		services.Received(1).Add(Arg.Any<ServiceDescriptor>());
		Assert.AreEqual(1, reposManager.Files.Length);
		Assert.IsNotNull(serviceDescriptor);
		Assert.AreEqual(ServiceLifetime.Transient, serviceDescriptor.Lifetime);
	}

	[TestMethod]
	public void Configure_When_UpdateTasksOptionsSet_Expect_CorrespondingUpdateTaskCount()
	{
		IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestUpdateTask));
		_reflectionUtility.ActivatorCreateInstance(Arg.Any<Type>(), Arg.Any<IServiceProvider>()).Returns(new TestUpdateTask());
		_reflectionUtility.GetShallowCopy(Arg.Any<CsvUpdateTaskOptions>()).Returns(x => x.Arg<CsvUpdateTaskOptions>());
		var options = new CsvReposOptions { ServiceProvider= serviceProvider, UpdateTasks = new[] { new CsvUpdateTaskOptions { TaskTypeName = nameof(TestUpdateTask) } } };

		reposManager.Configure(options);

		Assert.AreEqual(1, reposManager.UpdateTasks.Length);
	}

	[TestMethod]
	public void Configure_When_UpdateTasksOptionsSet_Expect_CorrespondingUpdateTask()
	{
		var serviceProvider = Substitute.For<IServiceProvider>();
		var reposManager = new CsvReposManager(_csvReaderFactory, new ReflectionUtility(), _fileSystem);
		CsvUpdateTaskOptions taskOptions = new();
		taskOptions.TaskTypeName = nameof(TestUpdateTask);
		taskOptions.AffectedTypeNames = new[] { nameof(TestItem) };
		taskOptions.IntervalInMinutes = 25;
		taskOptions.IsDisabled = true;
		taskOptions.Properties = new() { { "ThrowsError", "false" }, { "FilePath", "./test.csv" } };
		var options = new CsvReposOptions { UpdateTasks = new[] { taskOptions }, ServiceProvider = serviceProvider };

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
		var serviceProvider = Substitute.For<IServiceProvider>();
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestUpdateTask));
		_reflectionUtility.ActivatorCreateInstance(Arg.Any<Type>(), Arg.Any<IServiceProvider>()).Returns(new TestUpdateTask() { ThrowsError = true });
		_reflectionUtility.GetShallowCopy(Arg.Any<CsvUpdateTaskOptions>()).Returns(x => x.Arg<CsvUpdateTaskOptions>());
		var options = new CsvReposOptions 
		{
			ServiceProvider = serviceProvider,
			UpdateTasks = new[] { new CsvUpdateTaskOptions { TaskTypeName = "TestUpdateTask" } } 
		};

		reposManager.Configure(options);
		await reposManager.UpdateAsync();
	}

	[TestMethod]
	public async Task UpdateAsync_When_UpdateTaskStateIsErrorAndUpdateTaskErrorSet_Expect_ExceptionHandled()
	{
		var serviceProvider = Substitute.For<IServiceProvider>();
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		reposManager.UpdateTaskError += (s, e) => { };
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestUpdateTask));
		_reflectionUtility.ActivatorCreateInstance(Arg.Any<Type>(), Arg.Any<IServiceProvider>()).Returns(new TestUpdateTask() { ThrowsError = true });
		_reflectionUtility.GetShallowCopy(Arg.Any<CsvUpdateTaskOptions>()).Returns(x => x.Arg<CsvUpdateTaskOptions>());
		var options = new CsvReposOptions 
		{
			ServiceProvider = serviceProvider,
			UpdateTasks = new[] { new CsvUpdateTaskOptions { TaskTypeName = "TestUpdateTask" } } 
		};

		reposManager.Configure(options);
		await reposManager.UpdateAsync();
	}

	[TestMethod]
	public async Task UpdateAsync_When_UpdateTaskResultIsNotSuccess_Expect_TaskRunAfterIncrementByRetryInterval()
	{
		var serviceProvider = Substitute.For<IServiceProvider>();
		int retryInterval = 10;
		var reposManager = new CsvReposManager(_csvReaderFactory, _reflectionUtility, _fileSystem);
		reposManager.UpdateTaskError += (s,e) => { };
		_reflectionUtility.GetRequiredClassType(Arg.Any<string>()).Returns(typeof(TestUpdateTask));
		_reflectionUtility.ActivatorCreateInstance(Arg.Any<Type>(), Arg.Any<IServiceProvider>()).Returns(new TestUpdateTask() { ThrowsError = true });
		_reflectionUtility.GetShallowCopy(Arg.Any<CsvUpdateTaskOptions>()).Returns(x => x.Arg<CsvUpdateTaskOptions>());
		var options = new CsvReposOptions 
		{ 
			ServiceProvider = serviceProvider,
			RetryIntervalInMinutes = retryInterval, 
			UpdateTasks = new[] { new CsvUpdateTaskOptions { TaskTypeName = "TestUpdateTask" } } 
		};
		var minRetryTime = DateTime.Now.AddMinutes(retryInterval);

		reposManager.Configure(options);
		await reposManager.UpdateAsync();

		Assert.IsTrue(reposManager.UpdateTasks[0].RunAfter.Minute == minRetryTime.Minute);
	}
}
