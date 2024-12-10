# Code2.Tools.Csv.Repos
Tools and services to use and maintain csv repositories


## Options
- DefaultReaderOptions, fallback csv reader options (optional)
- CsvReadSize, csv reader read chunk size (default: 5000)
- UpdateIntervalInMinutes, update timer interval in minutes (default: 5)
- RetryIntervalInMinutes, fallback update task retry interval in minutes (default: 60)
- Files, array of csv file option objects
  - FilePath, csv file path
  - TypeName, type name for deserializing a csv line (optional)
  - Repository, repository instance implementing IRespository<> (optional)
  - CsvReaderOptions, file specific reader options (optional)
- UpdateTasks, array of csv update task option objects
  - TaskTypeName, type name of update task implementing ICsvUpdateTask (optional)
  - TaskType, type of update task implementing ICsvUpdateTask (optional)
  - AffectedTypeNames, list of affected file type names, when the task completes will trigger a reload corresponding repository (optional)
  - IntervalInMinutes, task run interval in minutes
  - RetryIntervalInMinutes, task run interval in minutes in case of an unsuccessful run (optional)
  - IsDisabled, indicates whether the task is disabled (default: false)
  - Properties, collection of task specific property values by name (optional)


## Example 
Configure and using csv data
```
/*
[sample file: people.csv]
id,first,last,age
1,don,joe,23
2,dane,joe,21
..
*/

// AIO wonder Program.cs
using Microsoft.Extensions.DependencyInjection;
using Code2.Tools.Csv.Repos;
using System;
using System.Linq;
using System.Collections.Generic;

//configure services
IServiceCollection services = new ServiceCollection();
services.AddCsvRepos(x => x.AddFile<Person>("./people.csv", readerOptions => readerOptions.HasHeaderRow = true));

//activate services
var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseCsvRepos(loadOnStart: true);

//use services
var repo = serviceProvider.GetRequiredService<ICsvRepository<Person>>();
var count = repo.Get().Count();
var first = repo.Get(x => x.Id == 2).First();

Console.WriteLine("count: {0}", count);
Console.WriteLine("first: {0}, {1} {2}", first.Id, first.First, first.Last);

//data item
public class Person
{
	public int Id { get; set; }
	public string First { get; set; } = string.Empty;
	public string Last { get; set; } = string.Empty;
	public int Age { get; set; }
}

//simple repositry using List<>
public class PeopleRepository : ICsvRepository<Person>
{
	private readonly List<Person> _people = new List<Person>();

	public void Add(IEnumerable<Person> items) => _people.AddRange(items);
	public void Clear() => _people.Clear();
	public IEnumerable<Person> Get(Func<Person, bool>? filter = null) => _people.Where(x => filter is null || filter(x)).ToArray();
}
```