using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos.Internals;
internal class HttpUtility : IHttpUtility
{
	public async Task DownloadFileToAsync(string url, Stream fileStream, Dictionary<string, string>? requestHeaders = null)
	{
		using var client = GetClient(requestHeaders);
		using Stream httpStream = await client.GetStreamAsync(url);

		httpStream.CopyTo(fileStream);
		httpStream.Close();
	}

	public async Task<byte[]> GetByteArrayAsync(string url, Dictionary<string, string>? requestHeaders = null)
	{
		using var client = GetClient(requestHeaders);
		return await client.GetByteArrayAsync(url);
	}

	private HttpClient GetClient(Dictionary<string, string>? requestHeaders = null)
	{
		var client = new HttpClient();
		if (requestHeaders is not null)
		{
			foreach (string key in requestHeaders.Keys)
			{
				client.DefaultRequestHeaders.Add(key, requestHeaders[key]);
			}
		}
		return client;
	}
}
