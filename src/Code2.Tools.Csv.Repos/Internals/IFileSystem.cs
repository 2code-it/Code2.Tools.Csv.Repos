using System;
using System.Collections.Generic;
using System.IO;

namespace Code2.Tools.Csv.Repos.Internals;

internal interface IFileSystem
{
	string PathCombine(params string[] paths);
	string PathGetFullPath(string path);
	void DirectoryCreate(string path);
	bool DirectoryExists(string path);
	string[] DirectoryGetFiles(string path, string search);
	string[] DirectoryGetFiles(string path);
	void FileDelete(string path);
	bool FileExists(string path);
	DateTime FileGetLastWriteTime(string path);
	void FileAppendAllLines(string path, IEnumerable<string> contents);
	Stream FileCreate(string path);
	void FileWriteAllBytes(string filePath, byte[] contents);
}