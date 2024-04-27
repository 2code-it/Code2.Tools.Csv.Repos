using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace Code2.Tools.Csv.Repos.Internals
{
	internal class ReflectionUtility : IReflectionUtility
	{
		public ReflectionUtility()
		{
			_nonSystemDomainTypes = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic).SelectMany(x => x.ExportedTypes.Where(NonSystemTypeFilter)).ToArray();
		}

		private readonly Type[] _nonSystemDomainTypes;
		private const string _repositoryInterfaceTypeName = "IRepository`1";

		public Type GetRequiredType(string typeName)
			=> _nonSystemDomainTypes.FirstOrDefault(x => x.Name == typeName) ?? throw new InvalidOperationException($"Required type '{typeName}' not found");

		public bool HasConstructorFor(Type type, Type[] constructorParams)
		{
			return type.GetConstructors().Where(x => x.IsPublic).Select(x => x.GetParameters().Select(y => y.ParameterType).ToArray())
				.Where(x => x.Length == constructorParams.Length)
				.Any(x => x.Select((t, i) => t == constructorParams[i] ? 1 : 0).Sum() == constructorParams.Length);
		}


		public Type GetRepositoryType(string repositoryTypeName, string? genericParameterTypeName = null)
		{
			Type? repositoryType = _nonSystemDomainTypes.FirstOrDefault(x => x.Name == repositoryTypeName);
			if (repositoryType is null) throw new InvalidOperationException($"Repository type '{repositoryTypeName}' not found");
			if (repositoryType.GetInterface(_repositoryInterfaceTypeName) is null) throw new InvalidOperationException($"Type '{repositoryTypeName}' is not assignable to IRepository<>");
			if (!repositoryType.IsGenericType) return repositoryType;
			if (genericParameterTypeName is null) throw new InvalidOperationException($"Generic parameter type name is required");
			Type genericParameterType = GetRequiredType(genericParameterTypeName);
			return repositoryType.MakeGenericType(genericParameterType);
		}

		public Type GetRepositoryInterfaceType(string repositoryTypeName, string? genericParameterTypeName = null)
			=> GetRepositoryInterfaceType(GetRepositoryType(repositoryTypeName, genericParameterTypeName));

		public Type GetRepositoryInterfaceType(Type repositoryType)
			=> repositoryType.GetInterface(_repositoryInterfaceTypeName) ?? throw new InvalidOperationException($"{repositoryType} does not implement IRepository");

		public void SetProperties(object instance, IDictionary<string, string> properties)
		{
			PropertyInfo[] propertyInfos = instance.GetType().GetProperties().Where(x => x.CanWrite && properties.Keys.Contains(x.Name, StringComparer.InvariantCultureIgnoreCase)).ToArray();
			foreach (PropertyInfo propertyInfo in propertyInfos)
			{
				string stringValue = properties[propertyInfo.Name];

				try
				{
					object value = Convert.ChangeType(stringValue, propertyInfo.PropertyType);
					propertyInfo.SetValue(instance, value, null);
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException($"Failed to set value for property {propertyInfo.Name}", ex);
				}
			}
		}

		public void InvokePrivateGenericMethod(object instance, string methodName, Type genericArgumentType, object[] parameters)
		{
			MethodInfo? methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (methodInfo is null) throw new InvalidOperationException($"Method '{methodName}' not fond");
			if (!methodInfo.IsGenericMethod) throw new InvalidOperationException("Method is not generic");

			methodInfo.MakeGenericMethod(genericArgumentType).Invoke(instance, parameters);
		}

		private static bool NonSystemTypeFilter(Type type)
		{
			if (type.Namespace is null) return false;
			if (type.Namespace.StartsWith("System")) return false;
			if (type.Namespace.StartsWith("Microsoft")) return false;
			return true;
		}
	}
}
