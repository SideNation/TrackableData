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
                        // fully-qualified (global::) so emitted type names never collide with an
                        // enclosing namespace (e.g. MongoDB.Bson vs the TrackableData.MongoDB plugin)
                        prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        prop.Type,
                        IsTrackableType(prop.Type)));
                }
            }
            return properties;
        }

        // Robust match for a well-known generic TrackableData interface.
        // OriginalDefinition.ToDisplayString() renders type parameters as "<T>" (not metadata "`1"),
        // so compare by simple name + arity + namespace instead.
        private static bool IsWellKnownGeneric(INamedTypeSymbol iface, string name)
        {
            var def = iface.OriginalDefinition;
            return def.Name == name
                && def.TypeParameters.Length >= 1
                && def.ContainingNamespace != null
                && def.ContainingNamespace.ToDisplayString() == "TrackableData";
        }

        public static bool IsTrackablePoco(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType)
            {
                foreach (var iface in namedType.AllInterfaces)
                {
                    if (IsWellKnownGeneric(iface, "ITrackablePoco"))
                        return true;
                }
            }
            return false;
        }

        // A poco container member is declared as its interface (IXxx); the backing field needs
        // the concrete generated trackable (TrackableXxx), which lives in the same namespace.
        public static string GetConcreteTrackableTypeName(ITypeSymbol typeSymbol)
        {
            var ns = typeSymbol.ContainingNamespace;
            var nsName = ns == null || ns.IsGlobalNamespace ? "" : ns.ToDisplayString();
            var name = "Trackable" + typeSymbol.Name.Substring(1);
            return string.IsNullOrEmpty(nsName) ? $"global::{name}" : $"global::{nsName}.{name}";
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
                    if (IsWellKnownGeneric(iface, "ITrackable") ||
                        IsWellKnownGeneric(iface, "ITrackablePoco") ||
                        IsWellKnownGeneric(iface, "ITrackableContainer"))
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
                    if (IsWellKnownGeneric(iface, "ITrackablePoco"))
                    {
                        var pocoType = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        return $"global::TrackableData.TrackablePocoTracker<{pocoType}>";
                    }
                    if (IsWellKnownGeneric(iface, "ITrackableContainer"))
                    {
                        // Container tracker is named Trackable{Name}Tracker
                        var containerType = iface.TypeArguments[0];
                        var trackerName = "Trackable" + containerType.Name.Substring(1) + "Tracker";
                        var ns = GetFullNamespace((INamedTypeSymbol)containerType);
                        return string.IsNullOrEmpty(ns) ? $"global::{trackerName}" : $"global::{ns}.{trackerName}";
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
