using Code2.Tools.Csv.Repos;
using Code2.Tools.Csv.Repos.Internals;
using Code2.Tools.Csv.ReposTests.Assets;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Code2.Tools.Csv.ReposTests.Internals;

[TestClass]
public class ReflectionUtilityTests
{
	[TestMethod]
	public void GetRequiredType_When_TypeExists_Expect_NoException()
	{
		ReflectionUtility reflectionUtility = new();

		reflectionUtility.GetRequiredClassType(nameof(TestItem));
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void GetRequiredType_When_TypeNotExists_Expect_Exception()
	{
		ReflectionUtility reflectionUtility = new();

		reflectionUtility.GetRequiredClassType("TestItemNonExisting");
	}

	[TestMethod]
	public void GetGenericInterface_When_SourceIsGeneric_Expect_SameGenericArgument()
	{
		ReflectionUtility reflectionUtility = new();

		Type? subject = reflectionUtility.GetGenericInterface(typeof(TestRepositoryGeneric<TestItem>), typeof(ICsvRepository<>));
		Type? genericArg = subject?.GetGenericArguments().FirstOrDefault();

		Assert.IsNotNull(subject);
		Assert.IsNotNull(genericArg);
		Assert.AreEqual(typeof(TestItem), genericArg);
	}

	[TestMethod]
	public void GetShallowCopy_When_PropertyValueIsObject_Expect_ValueNotCopied()
	{
		ReflectionUtility reflectionUtility = new();
		CsvReposOptions options = new();
		options.DefaultReaderOptions = new CsvReaderOptions { Header = new[] { "name", "value" } };

		var result = reflectionUtility.GetShallowCopy(options);

		Assert.IsNull(result.DefaultReaderOptions);
	}

	[TestMethod]
	public void GetShallowCopy_When_CopiedArrayItemChanged_Expect_SourceArrayUnchanged()
	{
		ReflectionUtility reflectionUtility = new();
		string[] items = new[] { "name1", "name2", "name3" };
		var test1 = new TestWithArray<string>(items);
		string newValueAt1 = "name4";

		var test2 = reflectionUtility.GetShallowCopy(test1);
		test2.Items![1] = newValueAt1;

		Assert.AreNotEqual(newValueAt1, test1.Items![1]);
	}

	[TestMethod]
	public void GetShallowCopy_When_PropertyIsArray_Expect_ValueTypeOrStringArraysCopied()
	{
		ReflectionUtility reflectionUtility = new();
		TestWithArray<string> testString = new("item1", "item2");
		TestWithArray<bool> testBool = new(false, true);
		TestWithArray<TestItem> testObject = new(new(), new() { ByteValue = 1 });

		var testStringCopied = reflectionUtility.GetShallowCopy(testString);
		var testBoolCopied = reflectionUtility.GetShallowCopy(testBool);
		var testObjectCopied = reflectionUtility.GetShallowCopy(testObject);

		Assert.AreEqual(testString.Items.Length, testStringCopied.Items.Length);
		Assert.AreEqual(testBool.Items.Length, testBoolCopied.Items.Length);
		Assert.AreEqual(0, testObjectCopied.Items.Length);
	}

	[TestMethod]
	[DataRow(typeof(string), true)]
	[DataRow(typeof(int), true)]
	[DataRow(typeof(string?[]), false)]
	[DataRow(typeof(object), false)]
	public void IsValueTypeOrString_When_ProvidedTypeIsValueTypeOrString_Expect_TrueElseFalse(Type type, bool expectedResult)
	{
		bool result = ReflectionUtility.IsValueTypeOrString(type);

		Assert.AreEqual(expectedResult, result);
	}

	[TestMethod]
	public void SetValueTypeOrStringProperties_When_PropertyValuesOrConformType_Expect_ValuesSet()
	{
		ReflectionUtility reflectionUtility = new();
		TestItem testItem = new();
		testItem.BoolValue = true;
		testItem.ByteValue = 22;
		testItem.DateTimeValue = new DateTime(2000, 1, 1, 22, 10, 10);
		testItem.IntValue = 10;
		testItem.UIntValue = 10;
		testItem.StringValue = "test";
		Dictionary<string, string> propertyValues = new();
		propertyValues[nameof(TestItem.BoolValue)] = testItem.BoolValue.ToString();
		propertyValues[nameof(TestItem.ByteValue)] = testItem.ByteValue.ToString();
		propertyValues[nameof(TestItem.DateTimeValue)] = testItem.DateTimeValue.ToString("s");
		propertyValues[nameof(TestItem.IntValue)] = testItem.IntValue.ToString();
		propertyValues[nameof(TestItem.UIntValue)] = testItem.UIntValue.ToString();
		propertyValues[nameof(TestItem.StringValue)] = testItem.StringValue.ToString();
		TestItem result = new();

		reflectionUtility.SetValueTypeOrStringProperties(result, propertyValues);

		Assert.AreEqual(testItem.BoolValue, result.BoolValue);
		Assert.AreEqual(testItem.ByteValue, result.ByteValue);
		Assert.AreEqual(testItem.DateTimeValue, result.DateTimeValue);
		Assert.AreEqual(testItem.IntValue, result.IntValue);
		Assert.AreEqual(testItem.UIntValue, result.UIntValue);
		Assert.AreEqual(testItem.StringValue, result.StringValue);
	}

	[TestMethod]
	[DataRow(typeof(TestRepository), null, typeof(ICsvRepository<TestItem>))]
	[DataRow(null, typeof(TestItem), typeof(ICsvRepository<TestItem>))]
	[DataRow(typeof(TestRepositoryFail), null, null)]
	[DataRow(null, typeof(TestItemFail), typeof(ICsvRepository<TestItemFail>))]
	public void GetRepositoryInterfaceType_When_UsingInstanceOrTypeName_Expect_ExpectedInterfaceType(Type? repoType, Type? itemType, Type expectedInterfaceType)
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		object? instance = repoType is null ? null : Activator.CreateInstance(repoType);

		Type? interfaceType = null;
		try
		{
			interfaceType = reflectionUtility.GetRepositoryInterfaceType(instance, itemType);
		}
		catch
		{
		}

		Assert.AreEqual(expectedInterfaceType, interfaceType);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void GetRepositoryInterfaceType_When_InstanceDoesntImplementIRepository_Expect_Exception()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		object instance = new TestRepositoryFail();

		Type interfaceType = reflectionUtility.GetRepositoryInterfaceType(instance, null);

		Assert.AreEqual(typeof(ICsvRepository<TestItem>), interfaceType);
	}

	[TestMethod]
	[DataRow(typeof(TestRepository), null, typeof(TestRepository))]
	[DataRow(null, typeof(TestItem), typeof(TestRepository))]
	[DataRow(typeof(TestRepositoryFail), null, typeof(TestRepositoryFail))]
	public void GetRepositoryImplementationType_When_UsingInstanceOrTypeName_Expect_ExpectedInterfaceType(Type? repoType, Type? itemType, Type expectedImplementationType)
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		object? instance = repoType is null ? null : Activator.CreateInstance(repoType);

		Type implementationType = reflectionUtility.GetRepositoryImplementationType(instance, itemType);

		Assert.AreEqual(expectedImplementationType, implementationType);
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public void GetRepositoryImplementationType_When_UsingInvalidTypeName_Expect_Exception()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		Type itemType = typeof(TestItemFail);

		reflectionUtility.GetRepositoryImplementationType(null, itemType);
	}

	[TestMethod]
	[DataRow(typeof(TestRepository), null, typeof(TestRepository))]
	[DataRow(null, typeof(TestItem), typeof(TestRepository))]
	[DataRow(typeof(TestRepositoryFail), null, typeof(TestRepositoryFail))]
	public void GetOrCreateRepository_When_UsingInstanceOrTypeName_Expect_ExpectedInterfaceType(Type? repoType, Type? itemType, Type expectedImplementationType)
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		object? instance = repoType is null ? null : Activator.CreateInstance(repoType);

		object? repoInstance = reflectionUtility.GetOrCreateRepository(instance, itemType);

		Assert.IsNotNull(repoInstance);
		Assert.AreEqual(repoInstance.GetType(), expectedImplementationType);
	}

	[TestMethod]
	public void GetOrCreateRepository_When_UsingServiceProvider_Expect_ServiceProviderGetServiceCall()
	{
		ReflectionUtility reflectionUtility = new ReflectionUtility();
		IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
		serviceProvider.GetService(typeof(ICsvRepository<TestItem>)).Returns(new TestRepository());

		object? repoInstance = reflectionUtility.GetOrCreateRepository(null, typeof(TestItem), serviceProvider);

		serviceProvider.Received(1).GetService(Arg.Any<Type>());
	}

	private class TestWithArray<T>
	{
		public TestWithArray() { }
		public TestWithArray(params T[] items) { Items = items; }
		public T[] Items { get; set; } = Array.Empty<T>();
	}

}
