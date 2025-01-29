using Code2.Tools.Csv.Repos;
using Code2.Tools.Csv.ReposTests.Assets;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace Code2.Tools.Csv.ReposTests;

[TestClass]
public class DepencyInjectionTests
{
	[TestMethod]
	public void AddCsvRepos_When_FileOptionsTypeIsSet_Expect_IRepositoryServiceType()
	{
		IServiceCollection services = Substitute.For<IServiceCollection>();
		List<Type> serviceTypes = new();
		services.When(x => x.Add(Arg.Any<ServiceDescriptor>())).Do(x => serviceTypes.Add(x.Arg<ServiceDescriptor>().ServiceType));

		services.AddCsvRepos(x => x.Files = new[] { new CsvFileOptions { ItemTypeName = nameof(TestItem) } });

		Assert.IsTrue(serviceTypes.Contains(typeof(ICsvRepository<TestItem>)));
	}

	[TestMethod]
	public void AddCsvRepos_When_Added_Expect_DependenciesAdded()
	{
		IServiceCollection services = Substitute.For<IServiceCollection>();
		List<Type> serviceTypes = new();
		services.When(x => x.Add(Arg.Any<ServiceDescriptor>())).Do(x => serviceTypes.Add(x.Arg<ServiceDescriptor>().ServiceType));

		services.AddCsvRepos(x => x.ReaderReadSize = 1000);

		Assert.IsTrue(serviceTypes.Contains(typeof(ICsvReposManager)));
	}


	[TestMethod]
	[DataRow(true, true)]
	[DataRow(false, false)]
	public void UseCsvRepos_When_Invoked_Expect_ReposManagerConfigureInvoked(bool updateOnStart, bool loadOnStart)
	{
		IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
		ICsvReposManager reposManager = Substitute.For<ICsvReposManager>();
		serviceProvider.GetService(typeof(ICsvReposManager)).Returns(reposManager);

		serviceProvider.UseCsvRepos(updateOnStart, loadOnStart);

		reposManager.Received(1).Configure(Arg.Any<Action<CsvReposOptions>>());
		if (updateOnStart) reposManager.Received(1).UpdateAsync();
		if (loadOnStart) reposManager.Received(1).LoadAsync();
	}
}
