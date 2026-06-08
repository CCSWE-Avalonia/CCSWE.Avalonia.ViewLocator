using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CCSWE.Avalonia.ViewLocator.Generator;

/// <summary>
/// Emits an Avalonia <c>IDataTemplate</c> implementation for each partial class marked with
/// <c>[GenerateViewLocator]</c>, mapping <c>XxxViewModel</c> to a same-namespace <c>XxxView</c> that derives
/// from <c>Avalonia.Controls.Control</c>.
/// </summary>
[Generator]
public sealed class ViewLocatorGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "CCSWE.Avalonia.ViewLocator.GenerateViewLocatorAttribute";
    private const string ControlMetadataName = "Avalonia.Controls.Control";
    private const string GlobalPrefix = "global::";
    private const string ViewModelSuffix = "ViewModel";
    private const string ViewSuffix = "View";

    private static readonly SymbolDisplayFormat FullyQualified = SymbolDisplayFormat.FullyQualifiedFormat;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeMetadataName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => Parse(ctx))
            .Where(static target => target is not null)
            .WithTrackingName("ViewLocatorTargets");

        context.RegisterSourceOutput(
            targets.Combine(context.CompilationProvider),
            static (spc, pair) => Execute(spc, pair.Left!, pair.Right));
    }

    private static ViewLocatorTarget Parse(GeneratorAttributeSyntaxContext context)
    {
        var symbol = (INamedTypeSymbol) context.TargetSymbol;
        var declaration = (ClassDeclarationSyntax) context.TargetNode;

        string? viewModelBase = null;
        if (context.Attributes[0].ConstructorArguments is [{ Value: INamedTypeSymbol baseType }])
        {
            viewModelBase = baseType.ToDisplayString(FullyQualified);
        }

        return new ViewLocatorTarget
        {
            ClassName = symbol.Name,
            HintName = symbol.ToDisplayString(FullyQualified)[GlobalPrefix.Length..],
            IsPartial = declaration.Modifiers.Any(SyntaxKind.PartialKeyword),
            Namespace = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString(),
            ViewModelBaseFullyQualified = viewModelBase,
        };
    }

    private static void Execute(SourceProductionContext context, ViewLocatorTarget target, Compilation compilation)
    {
        if (!target.IsPartial)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NotPartial, Location.None, target.ClassName));
            return;
        }

        var controlSymbol = compilation.GetTypeByMetadataName(ControlMetadataName);
        if (controlSymbol is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.AvaloniaNotReferenced, Location.None));
            return;
        }

        var viewModelBase = target.ViewModelBaseFullyQualified is { } baseName
            ? compilation.GetTypeByMetadataName(baseName[GlobalPrefix.Length..])
            : null;

        var pairs = new List<(string ViewModel, string View)>();
        foreach (var viewModel in EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            if (viewModel.TypeKind != TypeKind.Class || viewModel.IsAbstract || viewModel.IsStatic
                || !viewModel.TypeParameters.IsEmpty
                || viewModel.Name.Length <= ViewModelSuffix.Length
                || !viewModel.Name.EndsWith(ViewModelSuffix, System.StringComparison.Ordinal)
                || (viewModelBase is not null && !InheritsOrEquals(viewModel, viewModelBase)))
            {
                continue;
            }

            var viewName = viewModel.Name[..^ViewModelSuffix.Length] + ViewSuffix;
            var namespacePrefix = viewModel.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : viewModel.ContainingNamespace.ToDisplayString() + ".";

            if (compilation.GetTypeByMetadataName(namespacePrefix + viewName) is { } view
                && InheritsOrEquals(view, controlSymbol))
            {
                pairs.Add((viewModel.ToDisplayString(FullyQualified), view.ToDisplayString(FullyQualified)));
            }
        }

        if (pairs.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NoMappings, Location.None, target.ClassName));
        }

        pairs.Sort(static (a, b) => string.CompareOrdinal(a.ViewModel, b.ViewModel));

        context.AddSource($"{target.HintName}.ViewLocator.g.cs", SourceText.From(Emit(target, pairs), Encoding.UTF8));
    }

    private static string Emit(ViewLocatorTarget target, List<(string ViewModel, string View)> pairs)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated/>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        if (target.Namespace is not null)
        {
            builder.AppendLine($"namespace {target.Namespace};");
            builder.AppendLine();
        }

        builder.AppendLine($"partial class {target.ClassName} : global::Avalonia.Controls.Templates.IDataTemplate");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly global::System.IServiceProvider _services;");
        builder.AppendLine();
        builder.AppendLine($"    public {target.ClassName}(global::System.IServiceProvider services) => _services = services;");
        builder.AppendLine();
        builder.AppendLine("    public global::Avalonia.Controls.Control? Build(object? data) =>");
        builder.AppendLine("        global::CCSWE.Avalonia.ViewLocator.ViewLocatorResolver.Build(data, _services, GetViewType);");
        builder.AppendLine();
        builder.AppendLine(target.ViewModelBaseFullyQualified is { } viewModelBase
            ? $"    public bool Match(object? data) => data is {viewModelBase};"
            : "    public bool Match(object? data) => data is not null && GetViewType(data.GetType()) is not null;");
        builder.AppendLine();
        builder.AppendLine("    private static global::System.Type? GetViewType(global::System.Type viewModelType)");
        builder.AppendLine("    {");

        foreach (var (viewModel, view) in pairs)
        {
            builder.AppendLine($"        if (viewModelType == typeof({viewModel}))");
            builder.AppendLine($"            return typeof({view});");
        }

        builder.AppendLine("        return null;");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static IEnumerable<INamedTypeSymbol> EnumerateTypes(INamespaceSymbol @namespace)
    {
        foreach (var member in @namespace.GetMembers())
        {
            if (member is INamespaceSymbol childNamespace)
            {
                foreach (var nested in EnumerateTypes(childNamespace))
                {
                    yield return nested;
                }
            }
            else if (member is INamedTypeSymbol type)
            {
                yield return type;
            }
        }
    }

    private static bool InheritsOrEquals(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }
}
