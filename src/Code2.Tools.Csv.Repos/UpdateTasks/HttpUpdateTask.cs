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
		await _semaphore.WaitAsync(cancellationToken);
		if (cancellationToken.IsCancellationRequested) return Result.Cancel();

		IResult? result = null;
		try
		{
			IsRunning = true;
			result = OnBeforeRun();
			if (result is not null && !result.IsSuccess) return result;

			if (Url is null) return Result.Error($"{nameof(Url)} is not defined");
			if (FilePath is null) return Result.Error($"{nameof(FilePath)} is not defined");

			string filePath = PathGetFullPath(FilePath);
			if (FileExists(filePath)) FileDelete(filePath);
			using (Stream fileStream = FileCreate(filePath))
			{
				await DownloadFileToAsync(Url, fileStream, RequestHeaders);
				await fileStream.FlushAsync(cancellationToken);
				fileStream.Close();
			}
			result = OnAfterRun();
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

	protected virtual async Task DownloadFileToAsync(string url, Stream fileStream, Dictionary<string, string>? headers = null)
		=> await _httpUtility.DownloadFileToAsync(url, fileStream, headers);

	protected virtual async Task<byte[]> HttpGetByteArrayAsync(string url, Dictionary<string, string>? headers = null)
		=> await _httpUtility.GetByteArrayAsync(url, headers);

	protected virtual async Task<Dictionary<string, string>> HttpGetHeadersOnlyAsync(string url, Dictionary<string, string>? requestHeaders = null)
		=> await _httpUtility.GetHeadersOnlyAsync(url, requestHeaders);

	protected virtual string PathGetFullPath(string filePath)
		=> _fileSystem.PathGetFullPath(filePath);

	protected virtual string PathCombine(params string[] paths)
		=> _fileSystem.PathCombine(paths);

	protected virtual bool FileExists(string filePath)
		=> _fileSystem.FileExists(filePath);

	protected virtual void FileDelete(string filePath)
		=> _fileSystem.FileDelete(filePath);

	protected virtual Stream FileCreate(string filePath)
		=> _fileSystem.FileCreate(filePath);

	protected virtual void FileWriteAllBytes(string filePath, byte[] contents)
		=> _fileSystem.FileWriteAllBytes(filePath, contents);

	protected virtual DateTime FileLastWriteTime(string filePath)
		=> _fileSystem.FileGetLastWriteTime(filePath);

	protected virtual void DirectoryCreate(string path)
		=> _fileSystem.DirectoryCreate(path);

	protected virtual bool DirectoryExists(string path)
		=> _fileSystem.DirectoryExists(path);
}
