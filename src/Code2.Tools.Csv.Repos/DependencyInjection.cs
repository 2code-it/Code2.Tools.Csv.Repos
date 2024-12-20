using Microsoft.Extensions.DependencyInjection;
using System;

namespace Code2.Tools.Csv.Repos;
public static class DependencyInjection
{
	public static IServiceCollection AddCsvRepos(this IServiceCollection services, Action<CsvReposOptions> config)
	{
		CsvReposOptions options = new();
		config(options);
		return services.AddCsvRepos(options);
	}

	public static IServiceCollection AddCsvRepos(this IServiceCollection services, CsvReposOptions options)
	{
		var csvReposManager = new CsvReposManager(new CsvReaderFactory());
		options.ServiceCollection = services;
		csvReposManager.Configure(options);
		services.AddSingleton<ICsvReposManager>(csvReposManager);
		return services;
	}

	public static IServiceProvider UseCsvRepos(this IServiceProvider serviceProvider, bool updateOnStart = false, bool loadOnStart = false)
	{
		var reposManager = serviceProvider.GetRequiredService<ICsvReposManager>();
		reposManager.Configure(x => x.ServiceProvider = serviceProvider);

		if (updateOnStart) reposManager.UpdateAsync().Wait();
		if (loadOnStart) reposManager.LoadAsync().Wait();

		return serviceProvider;
	}
}
