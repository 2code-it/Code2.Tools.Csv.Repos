using Code2.Tools.Csv.Repos.Internals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos.UpdateTasks;
public class HttpUpdateTask : ICsvUpdateTask
{
	public HttpUpdateTask() : this(new HttpUtility(), new FileSystem())
	{ }
	internal HttpUpdateTask(IHttpUtility httpUtility, IFileSystem fileSystem)
	{
		_httpUtility = httpUtility;
		_fileSystem = fileSystem;
	}

	private readonly IHttpUtility _httpUtility;
	private readonly IFileSystem _fileSystem;
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public int IntervalInMinutes { get; set; }
	public int? RetryIntervalInMinutes { get; set; }
	public DateTime RunAfter { get; set; }
	public bool IsRunning { get; private set; }
	public bool IsDisabled { get; set; }
	public Type[]? AffectedTypes { get; set; }

	public string? Url { get; set; }
	public string? FilePath { get; set; }
	public Dictionary<string, string>? RequestHeaders { get; set; }

	public async Task<IResult> RunAsync(CancellationToken cancellationToken = default)
	{
		if (Url is null) return Result.Error($"{nameof(Url)} is not defined");
		if (FilePath is null) return Result.Error($"{nameof(FilePath)} is not defined");

		await _semaphore.WaitAsync(cancellationToken);
		if (cancellationToken.IsCancellationRequested) return Result.Cancel();

		IResult? result = null;
		try
		{
			IsRunning = true;
			result = OnBeforeRun();
			if (result is null || result.IsSuccess)
			{
				string filePath = PathGetFullPath(FilePath);
				if (FileExists(filePath)) FileDelete(filePath);
				using (Stream fileStream = FileCreate(filePath))
				{
					await DownloadFileToAsync(Url, fileStream, RequestHeaders);
					await fileStream.FlushAsync(cancellationToken);
					fileStream.Close();
				}
				OnAfterRun();
			}
		}
		catch (Exception ex)
		{
			result = Result.Error($"Update task '{GetType().Name}' failed", ex);
		}
		finally
		{
			_semaphore.Release();
			IsRunning = false;
		}
		return result ?? Result.Success();
	}

	protected virtual IResult? OnBeforeRun() { return null; }
	protected virtual IResult? OnAfterRun() { return null; }

	protected async Task DownloadFileToAsync(string url, Stream fileStream, Dictionary<string, string>? headers = null)
		=> await _httpUtility.DownloadFileToAsync(url, fileStream, headers);

	protected async Task<byte[]> GetByteArrayAsync(string url, Dictionary<string, string>? headers = null)
		=> await _httpUtility.GetByteArrayAsync(url, headers);

	protected async Task<Dictionary<string, string>> GetHeadersOnly(string url, Dictionary<string, string>? requestHeaders = null)
		=> await _httpUtility.GetHeadersOnly(url, requestHeaders);

	protected string PathGetFullPath(string filePath)
		=> _fileSystem.PathGetFullPath(filePath);

	protected string PathCombine(params string[] paths)
		=> _fileSystem.PathCombine(paths);

	protected bool FileExists(string filePath)
		=> _fileSystem.FileExists(filePath);

	protected void FileDelete(string filePath)
		=> _fileSystem.FileDelete(filePath);

	protected Stream FileCreate(string filePath)
		=> _fileSystem.FileCreate(filePath);

	protected void FileWriteAllBytes(string filePath, byte[] contents)
		=> _fileSystem.FileWriteAllBytes(filePath, contents);

	protected DateTime FileLastWriteTime(string filePath)
		=> _fileSystem.FileGetLastWriteTime(filePath);

	protected void DirectoryCreate(string path)
		=> _fileSystem.DirectoryCreate(path);

	protected bool DirectoryExists(string path)
		=> _fileSystem.DirectoryExists(path);
}
