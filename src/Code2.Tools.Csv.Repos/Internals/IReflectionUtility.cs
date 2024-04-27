using System;
using System.Collections.Generic;

namespace Code2.Tools.Csv.Repos.Internals
{
	internal interface IReflectionUtility
	{
		Type GetRequiredType(string typeName);
		Type GetRepositoryType(string repositoryTypeName, string genericParameterTypeName);
		Type GetRepositoryInterfaceType(string repositoryTypeName, string genericParameterTypeName);
		Type GetRepositoryInterfaceType(Type repositoryType);
		void SetProperties(object instance, IDictionary<string, string> properties);
		bool HasConstructorFor(Type type, Type[] constructorParams);
		void InvokePrivateGenericMethod(object instance, string methodName, Type genericArgumentType, object[] parameters);
	}
}