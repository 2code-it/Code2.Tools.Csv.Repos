using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos.Internals;
internal class HttpUtility : IHttpUtility
{
	public async Task DownloadFileToAsync(string url, Stream fileStream, Dictionary<string, string>? requestHeaders = null)
	{
		using var httpClient = GetHttpClient(requestHeaders);
		using Stream httpStream = await httpClient.GetStreamAsync(url);

		httpStream.CopyTo(fileStream);
		httpStream.Close();
	}

	public async Task<byte[]> GetByteArrayAsync(string url, Dictionary<string, string>? requestHeaders = null)
	{
		using var httpClient = GetHttpClient(requestHeaders);
		return await httpClient.GetByteArrayAsync(url);
	}

	public async Task<Dictionary<string, string>> GetHeadersOnly(string url, Dictionary<string, string>? requestHeaders = null)
	{
		using var httpClient = GetHttpClient(requestHeaders);

		var requestMessage = new HttpRequestMessage(HttpMethod.Head, url);
		var response = await httpClient.SendAsync(requestMessage);
		if (!response.IsSuccessStatusCode) throw new InvalidOperationException($"Response error {response.StatusCode}, {response.ReasonPhrase}");
		return response.Content.Headers.ToDictionary(x => x.Key, x => string.Join(";", x.Value));
	}

	private HttpClient GetHttpClient(Dictionary<string, string>? requestHeaders = null)
	{
		var httpClient = new HttpClient();
		if (requestHeaders is not null)
		{
			foreach (string key in requestHeaders.Keys)
			{
				httpClient.DefaultRequestHeaders.Add(key, requestHeaders[key]);
			}
		}
		return httpClient;
	}
}
