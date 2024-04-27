using Code2.Tools.Csv.Repos.Internals;
using Code2.Tools.Csv.ReposTests;
using NSubstitute;
using System;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos.Tests
{
	[TestClass]
	public class CsvLoaderTests
	{

		private IServiceProvider _serviceProvider = default!;
		private ICsvReaderFactory _csvReaderFactory = default!;
		private IFileSystem _fileSystem = default!;
		private IReflectionUtility _reflectionUtility = default!;
		private CsvReposOptions _options = default!;

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public async Task LoadFileAsync_When_CsvFileInfoNotFound_Expect_Exception()
		{
			ResetDependencies();
			CsvFileInfo fileInfo = new CsvFileInfo { NameFilter = "options.csv" };
			CsvLoader csvLoader = new CsvLoader(_serviceProvider, _csvReaderFactory, _options, _reflectionUtility, _fileSystem);
			_fileSystem.DirectoryGetFiles(Arg.Any<string>()).Returns(Array.Empty<string>());

			await csvLoader.LoadFileAsync(fileInfo);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public async Task LoadFileAsync_When_CsvFileNotFound_Expect_Exception()
		{
			ResetDependencies();
			CsvLoader csvLoader = new CsvLoader(_serviceProvider, _csvReaderFactory, _options, _reflectionUtility, _fileSystem);
			_fileSystem.DirectoryGetFiles(Arg.Any<string>()).Returns(Array.Empty<string>());

			await csvLoader.LoadFileAsync(string.Empty);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public async Task LoadFileAsync_When_RepositoryNotAvailable_Expect_Exception()
		{
			ResetDependencies();
			CsvFileInfo fileInfo = new CsvFileInfo { NameFilter = "options.csv" };
			_serviceProvider.GetService(Arg.Any<Type>()).Returns(null);
			CsvLoader csvLoader = new CsvLoader(_serviceProvider, _csvReaderFactory, _options, _reflectionUtility, _fileSystem);
			_fileSystem.DirectoryGetFiles(Arg.Any<string>()).Returns(new[] { "/var/test/options.csv" });

			await csvLoader.LoadFileAsync(fileInfo);

			_serviceProvider.GetService(Arg.Any<Type>()).Received(1);
		}

		[TestMethod]
		public async Task LoadFileAsync_When_RepositoryAvailable_Expect_CsvReaderCreated()
		{
			ResetDependencies();
			_reflectionUtility.GetRepositoryInterfaceType(Arg.Any<string>(), Arg.Any<string>()).Returns(typeof(TestRepository<TestItem>));
			_reflectionUtility.GetRequiredType(Arg.Any<string>()).Returns(typeof(TestItem));
			CsvFileInfo fileInfo = new CsvFileInfo { NameFilter = "options.csv" };
			_serviceProvider.GetService(Arg.Any<Type>()).Returns(new TestRepository<TestItem>());
			CsvLoader csvLoader = new CsvLoader(_serviceProvider, _csvReaderFactory, _options, _reflectionUtility, _fileSystem);
			_fileSystem.DirectoryGetFiles(Arg.Any<string>()).Returns(new[] { "/var/test/options.csv" });

			await csvLoader.LoadFileAsync(fileInfo);

			_csvReaderFactory.Create<TestItem>(Arg.Any<string>(), Arg.Any<CsvReaderOptions>()).Received(1);
		}


		private void ResetDependencies()
		{
			_serviceProvider = Substitute.For<IServiceProvider>();
			_csvReaderFactory = Substitute.For<ICsvReaderFactory>();
			_fileSystem = Substitute.For<IFileSystem>();
			_reflectionUtility = Substitute.For<IReflectionUtility>();
			_options = new CsvReposOptions();
		}
	}
}