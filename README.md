# Code2.Tools.Csv.Repos
Tools and services to maintain csv repositories


## Options
- CsvDataDirectory, data directory path, default "./data"
- Files, list of csv file info objects
  - NameFilter, csv file name filter
  - TargetTypeName, type name for deseriliazing a csv line
  - RepositoryTypeName, name of repository type implementing IRespository, default "MemoryRepository`1"
  - CsvReaderOptions, file specific reader options
- UpdateTasks, list of csv update task info objects
  - IntervalInHours, time in hours between runs
  - TaskTypeName, name of update task type implementing ICsvUpdateTask
  - TaskProperties, dictionary of strings with the keys matching TaskType properties
  - ReloadTargetTypeNames, array of type names to reload the repository for after the task completed
- CsvReaderOptions, fallback CsvReaderOptions
- CsvReaderAmount, csv read amount
- LoadOnStart, indicator to load configured files on start
- UpdateOnstart, indicator to run configured update tasks on start

## Example
```
IServiceCollection services = new ServiceCollection();

services.AddCsvRepos(options => {
	options.CsvDataDirectory = "./csv";
	options.Files.Add(new CsvFileInfo { NameFilter = "products.csv", TargetTypeName = "Product" });
	options.UpdateTasks.Add(new CsvUpdateTaskInfo { IntervalInHours = 24, ReloadTargetTypeNames = new[] { "Product" }, TaskTypeName = "UpdateTaskProducts" });
	options.LoadOnStart = true;
	options.UpdateOnStart = true;
});

IServiceProvider serviceProvider = services.BuildServiceProvider();
await serviceProvider.UseCsvReposAsync();

IRepository<Product> productRepo = serviceProvider.GetRequiredService<IRepository<Product>>();
var latest = productRepo.Get(x => x.Lastmodified > DateTime.Now.AddDays(-1));
```