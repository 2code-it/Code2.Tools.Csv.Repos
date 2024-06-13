using Code2.Tools.Csv.Repos.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos
{
	public static class CsvReposExtensions
	{
		private static readonly ReflectionUtility _reflectionUtility = new ReflectionUtility();
		private static ICsvUpdater? _csvUpdater;

		public static void AddCsvRepos(this IServiceCollection services, CsvReposOptions options)
		{
			services.AddSingleton(options);
			services.AddSingleton<ICsvReaderFactory, CsvReaderFactory>();
			services.TryAddSingleton<ICsvLoader, CsvLoader>();
			services.TryAddSingleton<ICsvUpdater, CsvUpdater>();

			foreach (var fileInfo in options.Files)
			{
				Type repositoryType = _reflectionUtility.GetRepositoryType(fileInfo.RepositoryTypeName, fileInfo.TargetTypeName);
				Type interfaceType = _reflectionUtility.GetRepositoryInterfaceType(repositoryType)!;
				services.AddSingleton(interfaceType, repositoryType);
			}
		}

		public static void AddCsvRepos(this IServiceCollection services, Action<CsvReposOptions> configure)
		{
			CsvReposOptions options = new CsvReposOptions();
			configure(options);
			services.AddCsvRepos(options);
		}

		public async static Task UseCsvReposAsync(this IServiceProvider serviceProvider, bool? updateOnStart = null, bool? loadOnStart = null)
		{
			CsvReposOptions options = serviceProvider.GetRequiredService<CsvReposOptions>();
			if (updateOnStart is not null) options.UpdateOnStart = updateOnStart.Value;
			if (loadOnStart is not null) options.LoadOnStart = loadOnStart.Value;
			_csvUpdater = serviceProvider.GetRequiredService<ICsvUpdater>();
			if (options.UpdateOnStart)
			{
				await _csvUpdater.RunAllTasksAsync();
			}
			if (options.UpdateTasks.Count > 0)
			{
				_csvUpdater.Start();
			}
			if (options.LoadOnStart)
			{
				ICsvLoader csvLoader = serviceProvider.GetRequiredService<ICsvLoader>();
				await csvLoader.LoadAsync();
			}
		}
	}
}
