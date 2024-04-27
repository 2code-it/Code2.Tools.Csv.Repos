using Code2.Tools.Csv.Repos;
using Code2.Tools.Csv.Repos.Internals;
using System;
using System.Collections.Generic;

namespace Code2.Tools.Csv.ReposTests
{
	[TestClass]
	public class ReflectionUtilityTests
	{
		[TestMethod]
		public void GetRequiredType_When_TypeExists_Expect_NoException()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			Type item = reflectionUtility.GetRequiredType("TestItem");
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetRequiredType_When_TypeNotExists_Expect_Exception()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			Type item = reflectionUtility.GetRequiredType("TestItem2");
		}

		[TestMethod]
		[DataRow(new[] { typeof(string) }, true)]
		[DataRow(new[] { typeof(int) }, false)]
		[DataRow(new[] { typeof(string), typeof(int) }, true)]
		public void HasConstructorFor_When_ConstructorExists_Expect_True(Type[] types, bool expectedResult)
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			bool result = reflectionUtility.HasConstructorFor(typeof(TestItem), types);

			Assert.AreEqual(expectedResult, result);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetRepositoryType_When_NotTypeExists_Expect_Exception()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			reflectionUtility.GetRepositoryType("TestRepo", "TestItem");
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetRepositoryType_When_TypeNotIRepository_Expect_Exception()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			reflectionUtility.GetRepositoryType("TestItem", "TestItem");
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetRepositoryType_When_TypeIsGenericAndNoGenericParameter_Expect_Exception()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			reflectionUtility.GetRepositoryType("TestRepository");
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetRepositoryType_When_TypeIsGenericAndAndGenericParameterNotFound_Expect_Exception()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			reflectionUtility.GetRepositoryType("TestRepository", "NonExistingType");
		}

		[TestMethod]
		public void GetRepositoryType_When_TypeIsGenericAndCorrect_Expect_Type()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			Type type = reflectionUtility.GetRepositoryType("TestRepository`1", "TestItem");

			Type genericType = type.GetGenericTypeDefinition();
			Type genericParameterType = type.GetGenericArguments()[0];

			Assert.AreEqual(typeof(TestRepository<>), genericType);
			Assert.AreEqual(typeof(TestItem), genericParameterType);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void GetRepositoryInterfaceType_When_IRepositoryNotImplemented_Expect_Exception()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			reflectionUtility.GetRepositoryInterfaceType("TestItem");
		}

		[TestMethod]
		public void GetRepositoryInterfaceType_When_IRepositoryIsImplemented_Expect_Type()
		{
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			Type type = reflectionUtility.GetRepositoryInterfaceType("TestRepository`1", "TestItem");

			Type genericTypeDefinition = type.GetGenericTypeDefinition();
			Type genericArgument = type.GetGenericArguments()[0];

			Assert.AreEqual(genericTypeDefinition, typeof(IRepository<>));
			Assert.AreEqual(genericArgument, typeof(TestItem));
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void SetProperties_When_PropertyTypeMismatch_Expect_Exception()
		{
			Dictionary<string, string> dictioanry = new Dictionary<string, string>() { { "ByteValue", "x" } };
			ReflectionUtility reflectionUtility = new ReflectionUtility();
			TestItem item = new();

			reflectionUtility.SetProperties(item, dictioanry);
		}

		[TestMethod]
		public void SetProperties_When_PropertyTypeMatch_Expect_PropertySet()
		{
			byte byteValue = 1;
			int intValue = 10;
			DateTime dateTimeValue = new DateTime(2001, 1, 1);
			Dictionary<string, string> dictioanry = new Dictionary<string, string>() {
				{ "ByteValue", byteValue.ToString()},
				{ "IntValue" , intValue.ToString()},
				{ "DateTimeValue", dateTimeValue.ToString() }
			};
			ReflectionUtility reflectionUtility = new ReflectionUtility();
			TestItem item = new();

			reflectionUtility.SetProperties(item, dictioanry);

			Assert.AreEqual(dateTimeValue, item.DateTimeValue);
			Assert.AreEqual(byteValue, item.ByteValue);
			Assert.AreEqual(intValue, item.IntValue);
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void InvokePrivateGenericMethod_When_MethodNotFound_Expect_Exception()
		{
			TestItem item = new();
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			reflectionUtility.InvokePrivateGenericMethod(item, "NotFound", typeof(TestItem), Array.Empty<object>());
		}

		[TestMethod]
		public void InvokePrivateGenericMethod_When_Invoke_Expect_ExpectInvokeResult()
		{
			TestItem item = new();
			ReflectionUtility reflectionUtility = new ReflectionUtility();

			reflectionUtility.InvokePrivateGenericMethod(item, "TestMethod", typeof(TestItem), Array.Empty<object>());

			Assert.AreEqual(typeof(TestItem).Name, item.TestMethodTypeName);
		}

	}
}
