using System;
using System.Collections.Generic;
using System.IO;

namespace Code2.Tools.Csv.Repos.Internals;

internal class FileSystem : IFileSystem
{
	public string PathGetFullPath(string path)
		=> Path.GetFullPath(path);

	public string PathCombine(params string[] paths)
		=> Path.Combine(paths);

	public string[] DirectoryGetFiles(string path, string search)
		=> Directory.GetFiles(path, search);

	public string[] DirectoryGetFiles(string path)
		=> Directory.GetFiles(path);

	public void DirectoryCreate(string path)
		=> Directory.CreateDirectory(path);

	public bool DirectoryExists(string path)
		=> Directory.Exists(path);

	public void FileDelete(string path)
		=> File.Delete(path);

	public bool FileExists(string path)
		=> File.Exists(path);

	public DateTime FileGetLastWriteTime(string path)
		=> File.GetLastWriteTime(path);

	public void FileAppendAllLines(string path, IEnumerable<string> contents)
		=> File.AppendAllLines(path, contents);

	public Stream FileCreate(string path)
		=> File.Create(path);

	public void FileWriteAllBytes(string filePath, byte[] contents)
		=> File.WriteAllBytes(filePath, contents);
}
