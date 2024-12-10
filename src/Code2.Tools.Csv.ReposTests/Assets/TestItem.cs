using System;

namespace Code2.Tools.Csv.ReposTests.Assets
{
	public class TestItem
	{
		public TestItem() { }
		public TestItem(string stringValue) { }
		public TestItem(string stringValue, int intValue) { }

		public int IntValue { get; set; }
		public uint UIntValue { get; set; }
		public string? StringValue { get; set; }
		public byte ByteValue { get; set; }
		public bool BoolValue { get; set; }
		public DateTime DateTimeValue { get; set; }

		public string? TestMethodTypeName { get; set; }

		private void TestMethod<T>()
		{
			TestMethodTypeName = typeof(T).Name;
		}
	}
}
