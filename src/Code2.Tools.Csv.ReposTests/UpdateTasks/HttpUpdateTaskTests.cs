using Code2.Tools.Csv.Repos;
using Code2.Tools.Csv.Repos.Internals;
using Code2.Tools.Csv.Repos.UpdateTasks;
using NSubstitute;
using System.IO;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.ReposTests.UpdateTasks;
[TestClass]
public class HttpUpdateTaskTests
{
	private IHttpUtility _httpUtility = default!;
	private IFileSystem _fileSystem = default!;

	[TestInitialize]
	public void Initialize()
	{
		_httpUtility = Substitute.For<IHttpUtility>();
		_fileSystem = Substitute.For<IFileSystem>();
	}

	[TestMethod]
	public async Task RunAsync_When_UrlIsNull_Expect_ErrorResult()
	{
		HttpUpdateTask task = new(_httpUtility, _fileSystem);

		var result = await task.RunAsync();

		Assert.AreEqual(ResultState.Error, result.State);
	}

	[TestMethod]
	public async Task RunAsync_When_TargetFileExists_Expect_FileDelete()
	{
		HttpUpdateTask task = new(_httpUtility, _fileSystem);
		_fileSystem.FileExists(Arg.Any<string>()).Returns(true);
		_fileSystem.FileCreate(Arg.Any<string>()).Returns(Stream.Null);
		task.Url = string.Empty;
		task.FilePath = string.Empty;

		await task.RunAsync();

		_fileSystem.Received(1).FileDelete(Arg.Any<string>());
	}

	[TestMethod]
	public async Task RunAsync_When_UrlSet_Expect_UrlPropertyAsDownloadToArgument()
	{
		HttpUpdateTask task = new(_httpUtility, _fileSystem);
		_fileSystem.FileExists(Arg.Any<string>()).Returns(false);
		_fileSystem.FileCreate(Arg.Any<string>()).Returns(Stream.Null);
		task.FilePath = string.Empty;
		task.Url = "http://www.com";

		await task.RunAsync();

		await _httpUtility.Received(1).DownloadFileToAsync(task.Url, Arg.Any<Stream>());
	}

	[TestMethod]
	public async Task RunAsync_When_HeadersSet_Expect_HeadersPropertyAsDownloadToArgument()
	{
		HttpUpdateTask task = new(_httpUtility, _fileSystem);
		_fileSystem.FileExists(Arg.Any<string>()).Returns(false);
		_fileSystem.FileCreate(Arg.Any<string>()).Returns(Stream.Null);
		task.FilePath = string.Empty;
		task.Url = string.Empty;
		task.RequestHeaders = new() { { "Accept", "text/json" } };

		await task.RunAsync();

		await _httpUtility.Received(1).DownloadFileToAsync(Arg.Any<string>(), Arg.Any<Stream>(), task.RequestHeaders);
	}
}
