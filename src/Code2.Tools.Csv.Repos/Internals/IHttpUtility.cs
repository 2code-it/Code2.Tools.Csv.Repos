using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos.Internals;
internal interface IHttpUtility
{
	Task DownloadFileToAsync(string url, Stream fileStream, Dictionary<string, string>? requestHeaders = null);
	Task<byte[]> GetByteArrayAsync(string url, Dictionary<string, string>? requestHeaders = null);
}