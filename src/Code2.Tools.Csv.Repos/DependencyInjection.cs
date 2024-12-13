using Code2.Tools.Csv.Repos.Internals;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Code2.Tools.Csv.Repos;
public static class DependencyInjection
{
	private static readonly CsvReposOptions _csvReposOptions = new();

	public static IServiceCollection AddCsvRepos(this IServiceCollection services)
		=> AddCsvReposInternal(services, new ReflectionUtility());

	public static IServiceCollection AddCsvRepos(this IServiceCollection services, CsvReposOptions options)
	{
		CopyOptionsTo(options, _csvReposOptions, new ReflectionUtility());
		return AddCsvReposInternal(services, new ReflectionUtility());
	}

	public static IServiceCollection AddCsvRepos(this IServiceCollection services, Action<CsvReposOptions> config)
	{
		config(_csvReposOptions);
		return AddCsvReposInternal(services, new ReflectionUtility());
	}

	public static IServiceProvider UseCsvRepos(this IServiceProvider serviceProvider, bool updateOnStart = false, bool loadOnStart = false)
	{
		var reposManager = serviceProvider.GetRequiredService<ICsvReposManager>();
		reposManager.Configure(_csvReposOptions, serviceProvider);

		if (updateOnStart) reposManager.UpdateAsync().Wait();
		if (loadOnStart) reposManager.LoadAsync().Wait();

		return serviceProvider;
	}

	internal static IServiceCollection AddCsvReposInternal(IServiceCollection services, IReflectionUtility reflectionUtility)
	{
		if (_csvReposOptions.Files is not null)
		{
			foreach (var fileOptions in _csvReposOptions.Files)
			{
				Type? itemType = fileOptions.TypeName is null ? fileOptions.Type : reflectionUtility.GetRequiredClassType(fileOptions.TypeName);
				Type repoInterfaceType = reflectionUtility.GetRepositoryInterfaceType(fileOptions.Repository, itemType);
				Type repoImplementationType = reflectionUtility.GetRepositoryImplementationType(fileOptions.Repository, itemType);

				if (fileOptions.Repository is not null)
				{
					services.AddSingleton(repoInterfaceType, fileOptions.Repository);
				}
				else
				{
					services.AddSingleton(repoInterfaceType, repoImplementationType);
				}
			}
		}

		services.AddSingleton<ICsvReaderFactory, CsvReaderFactory>();
		services.AddSingleton<ICsvReposManager, CsvReposManager>();
		return services;
	}

	private static void CopyOptionsTo(CsvReposOptions source, CsvReposOptions destination, IReflectionUtility reflectionUtility)
	{
		destination.UpdateIntervalInMinutes = source.UpdateIntervalInMinutes;
		destination.ReaderReadSize = source.ReaderReadSize;
		if (source.DefaultReaderOptions is not null) destination.DefaultReaderOptions = reflectionUtility.GetShallowCopy(source.DefaultReaderOptions);
		if (source.Files is not null) destination.Files = source.Files.Select(reflectionUtility.GetShallowCopy).ToArray();
		if (source.UpdateTasks is not null) destination.UpdateTasks = source.UpdateTasks.Select(reflectionUtility.GetShallowCopy).ToArray();
	}

}
