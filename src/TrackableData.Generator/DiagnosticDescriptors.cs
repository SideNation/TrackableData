using Microsoft.CodeAnalysis;

namespace TrackableData.Generator
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "TrackableDataGenerator";

        public static readonly DiagnosticDescriptor MustBePartial = new DiagnosticDescriptor(
            id: "TRACK001",
            title: "Trackable type must be partial",
            messageFormat: "The type '{0}' must be declared as partial to use TrackableData source generation",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MustBeInterface = new DiagnosticDescriptor(
            id: "TRACK002",
            title: "ITrackablePoco must be an interface",
            messageFormat: "The type '{0}' implementing ITrackablePoco<T> must be an interface",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ContainerMustBeInterface = new DiagnosticDescriptor(
            id: "TRACK003",
            title: "ITrackableContainer must be an interface",
            messageFormat: "The type '{0}' implementing ITrackableContainer<T> must be an interface",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PropertyMustHaveGetterAndSetter = new DiagnosticDescriptor(
            id: "TRACK004",
            title: "Property must have getter and setter",
            messageFormat: "The property '{0}' in '{1}' must have both getter and setter",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
