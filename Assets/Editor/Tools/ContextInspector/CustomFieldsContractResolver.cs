using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using strange.extensions.injector;
using UnityEngine;

namespace Com.Udogames.Core.Editor.Tools.ContextInspector
{
  public class CustomFieldsContractResolver : DefaultContractResolver
  {
    private readonly HashSet<Type> _allowedTypes = new HashSet<Type>
    {
      typeof(Vector3),
      typeof(Vector2),
      typeof(Color),
      // Add other specific types you want fully serialized
    };

    private readonly List<string> _namespaceContainsFilter = new List<string>
    {
      ".Vo", // Example: all classes under this namespace
    };

    private readonly List<string> _endsWithFilter = new List<string>
    {
      "Vo", // Example: all classes under this namespace
    };

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
      var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

      var properties = type.GetProperties(flags)
        .Where(p => p.GetCustomAttribute<ObsoleteAttribute>() == null)
        .Where(p => p.GetCustomAttribute<Inject>() == null)
        .Select(p => SafeCreateProperty(p, memberSerialization))
        .Where(p => p != null)
        .ToList();

      var fields = type.GetFields(flags)
        .Where(f => f.GetCustomAttribute<ObsoleteAttribute>() == null)
        .Select(f => base.CreateProperty(f, memberSerialization))
        .ToList();

      var allProps = fields.Concat(properties).ToList();

      foreach (var prop in allProps)
      {
        prop.Writable = true;
        prop.Readable = true;

        Type propType = prop.PropertyType;

        // Always allow primitive, string, and enum types
        if (propType.IsPrimitive || propType == typeof(string) || propType.IsEnum)
          continue;

        // Allow all Dictionaries
        if (typeof(System.Collections.IDictionary).IsAssignableFrom(propType))
          continue;

        // Allow all Lists/Collections (excluding string & byte[])
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string) && propType != typeof(byte[]))
          continue;

        // Allow types from _allowedTypes list
        if (_allowedTypes.Contains(propType))
          continue;

        // Allow types from allowed namespaces
        if (_namespaceContainsFilter.Any(ns => propType.Namespace != null && propType.Namespace.Contains(ns)))
          continue;

        // Allow types from allowed namespaces
        if (_endsWithFilter.Any(ns => propType.Name != null && propType.Name.EndsWith(ns)))
          continue;

        // Skip UnityEngine.Object derived types
        if (typeof(UnityEngine.Object).IsAssignableFrom(propType))
        {
          prop.ShouldSerialize = instance => false;
          continue;
        }

        // For all other complex types, skip serialization (prevent deep graph)
        prop.ShouldSerialize = instance => false;
      }

      return allProps;
    }

    private JsonProperty SafeCreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
      try
      {
        return base.CreateProperty(member, memberSerialization);
      }
      catch (NotImplementedException)
      {
        return null; // Skip problematic property
      }
    }
  }
}