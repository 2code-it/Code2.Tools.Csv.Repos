using Code2.Tools.Csv.Repos;
using Code2.Tools.Csv.ReposTests.Assets;

namespace Code2.Tools.Csv.ReposTests;

[TestClass]
public class OptionsExtensionTests
{
	[TestMethod]
	public void AddFile_When_Invoked_ExpectFileAdded()
	{
		CsvReposOptions options = new();
		string filePath = "./file.txt";

		options.AddFile<TestItem>(filePath, readerOptions => readerOptions.HasHeaderRow = true);

		Assert.IsNotNull(options.Files);
		Assert.IsTrue(options.Files[0].ReaderOptions!.HasHeaderRow);
		Assert.AreEqual(typeof(TestItem), options.Files[0].ItemType);
		Assert.AreEqual(filePath, options.Files[0].FilePath);
	}
}