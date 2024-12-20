using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace Code2.Tools.Csv.Repos.Internals;

internal class ReflectionUtility : IReflectionUtility
{
	public ReflectionUtility()
	{
		_nonFrameworkAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(x => !x.IsDynamic && !IsFrameworkAssembly(x)).ToArray();
		_nonFrameworkClasses = _nonFrameworkAssemblies.SelectMany(x => x.ExportedTypes).Where(x => x.IsClass).ToArray();
	}

	private readonly Assembly[] _nonFrameworkAssemblies;
	private readonly Type[] _nonFrameworkClasses;

	public Type GetRequiredClassType(string classTypeName)
	{
		return _nonFrameworkClasses.FirstOrDefault(x => x.FullName == classTypeName || x.Name == classTypeName)
			?? throw new InvalidOperationException($"Required type '{classTypeName}' not found");
	}

	public Type[] GetClasses(Func<Type, bool>? filter)
		=> _nonFrameworkClasses.Where(x => filter is null || filter(x)).ToArray();

	public Type? GetGenericInterface(Type source, Type genericTypeDefinition)
		=> source.GetInterfaces().Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericTypeDefinition).FirstOrDefault();

	public object? InvokePrivateGenericMethod(object instance, string methodName, Type genericArgumentType, object?[] parameters)
	{
		MethodInfo? methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (methodInfo is null) throw new InvalidOperationException($"Method '{methodName}' not found");
		if (!methodInfo.IsGenericMethod) throw new InvalidOperationException("Method is not generic");

		return methodInfo.MakeGenericMethod(genericArgumentType).Invoke(instance, parameters);
	}

	public object ActivatorCreateInstance(Type type, IServiceProvider? serviceProvider = null)
	{
		if (serviceProvider is not null) return ActivatorUtilities.CreateInstance(serviceProvider, type);
		return Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create instance of type {type}");
	}

	public T GetShallowCopy<T>(T source) where T : new()
	{
		T result = new();
		var properties = typeof(T).GetProperties()
			.Where(x => x.CanRead && x.CanWrite && (IsValueTypeOrString(x.PropertyType) || IsArrayOfValueTypeOrstring(x.PropertyType)))
			.ToArray();
		foreach (var property in properties)
		{
			var value = property.GetValue(source);
			if (value is null) continue;
			if (property.PropertyType.IsArray)
			{
				value = typeof(ReflectionUtility).GetMethod(nameof(NewArray), BindingFlags.Static | BindingFlags.NonPublic)!
						.MakeGenericMethod(property.PropertyType.GetElementType()!)
						.Invoke(null, new[] { value });
			}
			property.SetValue(result, value);
		}
		return result;
	}

	public Dictionary<string, string> GetValueTypeOrStringProperties(object source, Func<PropertyInfo, bool>? filter = null)
		=> source.GetType().GetProperties().Where(x => x.CanRead && IsValueTypeOrString(x.PropertyType) && (filter is null || filter(x)))
			.Select(x => (name: x.Name, value: x.GetValue(source)))
			.Where(x => x.value is not null)
			.ToDictionary(x => x.name, x => Convert.ToString(x.value)!);

	public void SetValueTypeOrStringProperties(object source, Dictionary<string, string> propertyValues)
	{
		Type type = source.GetType();
		var properties = type.GetProperties().Where(x => x.CanWrite && IsValueTypeOrString(x.PropertyType)).ToArray();
		foreach (var property in properties)
		{
			if (!propertyValues.TryGetValue(property.Name, out var value)) continue;
			try
			{
				property.SetValue(source, Convert.ChangeType(value, property.PropertyType));
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"Failed to set property value for '{type.Name}.{property.Name}' to '{value?.Substring(0, 20)}'", ex);
			}
		}
	}

	public Type TypeMakeGeneric(Type genericType, params Type[] typeArguments)
		=> genericType.MakeGenericType(typeArguments);


	internal static bool IsValueTypeOrString(Type type)
	{
		type = Nullable.GetUnderlyingType(type) ?? type;
		return type.IsValueType || type == typeof(string);
	}

	internal static bool IsArrayOfValueTypeOrstring(Type type)
		=> type.IsArray && IsValueTypeOrString(type.GetElementType()!);

	internal static object NewArray<T>(object arrayObject)
		=> ((IEnumerable<T>)arrayObject).ToArray();

	private static bool IsFrameworkAssembly(Assembly assembly)
		=> assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product == "Microsoft® .NET";
}
