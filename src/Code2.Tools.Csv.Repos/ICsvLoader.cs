using System.Threading.Tasks;

namespace Code2.Tools.Csv.Repos
{
	public interface ICsvLoader
	{
		Task LoadAsync(string[]? targetTypes = null);
		Task LoadFileAsync(string fileNameOrFilter, bool clearRepository = true);
		Task LoadFileAsync(CsvFileInfo fileInfo, bool clearRepository = true);
	}
}