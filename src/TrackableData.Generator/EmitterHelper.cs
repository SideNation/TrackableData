using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TrackableData.Generator
{
    internal static class EmitterHelper
    {
        public static string GetFullNamespace(INamedTypeSymbol symbol)
        {
            var ns = symbol.ContainingNamespace;
            if (ns == null || ns.IsGlobalNamespace)
                return "";
            return ns.ToDisplayString();
        }

        public static List<PropertyInfo> GetProperties(INamedTypeSymbol interfaceSymbol)
        {
            var properties = new List<PropertyInfo>();
            foreach (var member in interfaceSymbol.GetMembers())
            {
                if (member is IPropertySymbol prop && prop.GetMethod != null && prop.SetMethod != null)
                {
                    properties.Add(new PropertyInfo(
                        prop.Name,
                        prop.Type.ToDisplayString(),
                        prop.Type,
                        IsTrackableType(prop.Type)));
                }
            }
            return properties;
        }

        public static bool IsTrackableType(ITypeSymbol type)
        {
            var name = type.Name;
            if (name.StartsWith("Trackable") || name.StartsWith("ITrackable"))
                return true;

            if (type is INamedTypeSymbol namedType)
            {
                foreach (var iface in namedType.AllInterfaces)
                {
                    var ifaceName = iface.OriginalDefinition.ToDisplayString();
                    if (ifaceName == "TrackableData.ITrackable`1" ||
                        ifaceName == "TrackableData.ITrackablePoco`1" ||
                        ifaceName == "TrackableData.ITrackableContainer`1")
                        return true;
                }
            }

            return false;
        }

        public static string GetTrackerTypeName(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var genericName = namedType.OriginalDefinition.ToDisplayString();
                var typeArgs = string.Join(", ", namedType.TypeArguments.Select(t => t.ToDisplayString()));

                if (genericName == "TrackableData.TrackableDictionary<TKey, TValue>")
                    return $"TrackableData.TrackableDictionaryTracker<{typeArgs}>";
                if (genericName == "TrackableData.TrackableList<T>")
                    return $"TrackableData.TrackableListTracker<{typeArgs}>";
                if (genericName == "TrackableData.TrackableSet<T>")
                    return $"TrackableData.TrackableSetTracker<{typeArgs}>";
            }

            // For ITrackablePoco types, tracker is TrackablePocoTracker<InterfaceType>
            if (typeSymbol is INamedTypeSymbol nt)
            {
                foreach (var iface in nt.AllInterfaces)
                {
                    if (iface.OriginalDefinition.ToDisplayString() == "TrackableData.ITrackablePoco`1")
                    {
                        var pocoType = iface.TypeArguments[0].ToDisplayString();
                        return $"TrackableData.TrackablePocoTracker<{pocoType}>";
                    }
                    if (iface.OriginalDefinition.ToDisplayString() == "TrackableData.ITrackableContainer`1")
                    {
                        // Container tracker is named Trackable{Name}Tracker
                        var containerType = iface.TypeArguments[0];
                        var trackerName = "Trackable" + containerType.Name.Substring(1) + "Tracker";
                        var ns = GetFullNamespace((INamedTypeSymbol)containerType);
                        return string.IsNullOrEmpty(ns) ? trackerName : $"{ns}.{trackerName}";
                    }
                }
            }

            return $"TrackableData.ITracker";
        }
    }

    internal class PropertyInfo
    {
        public string Name { get; }
        public string TypeName { get; }
        public ITypeSymbol TypeSymbol { get; }
        public bool IsTrackable { get; }

        public PropertyInfo(string name, string typeName, ITypeSymbol typeSymbol, bool isTrackable)
        {
            Name = name;
            TypeName = typeName;
            TypeSymbol = typeSymbol;
            IsTrackable = isTrackable;
        }
    }
}
