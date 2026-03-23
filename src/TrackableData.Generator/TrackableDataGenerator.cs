using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TrackableData.Generator
{
    [Generator(LanguageNames.CSharp)]
    public class TrackableDataGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find interfaces that inherit from ITrackablePoco<T>
            var pocoInterfaces = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is InterfaceDeclarationSyntax,
                transform: static (ctx, _) => GetPocoTarget(ctx))
                .Where(static x => x != null);

            var pocoWithCompilation = pocoInterfaces.Combine(context.CompilationProvider);
            context.RegisterSourceOutput(pocoWithCompilation,
                static (spc, source) => EmitPoco(spc, source.Left!, source.Right));

            // Find interfaces that inherit from ITrackableContainer<T>
            var containerInterfaces = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is InterfaceDeclarationSyntax,
                transform: static (ctx, _) => GetContainerTarget(ctx))
                .Where(static x => x != null);

            var containerWithCompilation = containerInterfaces.Combine(context.CompilationProvider);
            context.RegisterSourceOutput(containerWithCompilation,
                static (spc, source) => EmitContainer(spc, source.Left!, source.Right));
        }

        private static bool ImplementsInterface(INamedTypeSymbol symbol, string interfaceMetadataName)
        {
            foreach (var iface in symbol.AllInterfaces)
            {
                if (iface.OriginalDefinition.MetadataName == interfaceMetadataName)
                    return true;
            }
            // Also check direct interfaces (for the case where the symbol IS the interface)
            foreach (var iface in symbol.Interfaces)
            {
                if (iface.OriginalDefinition.MetadataName == interfaceMetadataName)
                    return true;
            }
            return false;
        }

        private static InterfaceDeclarationSyntax? GetPocoTarget(GeneratorSyntaxContext context)
        {
            var interfaceDecl = (InterfaceDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;
            if (symbol == null) return null;

            if (ImplementsInterface(symbol, "ITrackablePoco`1"))
                return interfaceDecl;

            return null;
        }

        private static InterfaceDeclarationSyntax? GetContainerTarget(GeneratorSyntaxContext context)
        {
            var interfaceDecl = (InterfaceDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;
            if (symbol == null) return null;

            if (ImplementsInterface(symbol, "ITrackableContainer`1"))
                return interfaceDecl;

            return null;
        }

        private static void EmitPoco(SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl, Compilation compilation)
        {
            var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;
            if (symbol == null) return;

            var emitter = new TrackablePocoEmitter(context, symbol, interfaceDecl);
            var source = emitter.Emit();
            if (source != null)
            {
                var className = "Trackable" + symbol.Name.Substring(1);
                context.AddSource($"{className}.g.cs", source);
            }
        }

        private static void EmitContainer(SourceProductionContext context, InterfaceDeclarationSyntax interfaceDecl, Compilation compilation)
        {
            var semanticModel = compilation.GetSemanticModel(interfaceDecl.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;
            if (symbol == null) return;

            var emitter = new TrackableContainerEmitter(context, symbol, interfaceDecl);
            var source = emitter.Emit();
            if (source != null)
            {
                var className = "Trackable" + symbol.Name.Substring(1);
                context.AddSource($"{className}.g.cs", source);
            }
        }
    }
}
