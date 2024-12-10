using System;
using System.Collections.Generic;
using System.Reflection;

namespace Code2.Tools.Csv.Repos.Internals;

internal interface IReflectionUtility
{
	Type GetRequiredClassType(string classTypeName);
	Type? GetGenericInterface(Type source, Type genericTypeDefinition);
	Type[] GetClasses(Func<Type, bool>? filter);
	object? InvokePrivateGenericMethod(object instance, string methodName, Type genericArgumentType, object[] parameters);
	object ActivatorCreateInstance(Type type, IServiceProvider? serviceProvider = null);
	T GetShallowCopy<T>(T source) where T : new();
	Dictionary<string, string> GetValueTypeOrStringProperties(object source, Func<PropertyInfo, bool>? filter = null);
	void SetValueTypeOrStringProperties(object source, Dictionary<string, string> propertyValues);
	object? GetOrCreateRepository(object? repoInstance, Type? itemType, IServiceProvider? serviceProvider = null);
	Type GetRepositoryImplementationType(object? repoInstance, Type? itemType);
	Type GetRepositoryInterfaceType(object? repoInstance, Type? itemType);
}