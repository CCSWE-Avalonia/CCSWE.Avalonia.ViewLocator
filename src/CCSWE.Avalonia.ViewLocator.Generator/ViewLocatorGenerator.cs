using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CCSWE.Avalonia.ViewLocator.Generator;

/// <summary>
/// Emits an Avalonia <c>IDataTemplate</c> implementation for each partial class marked with
/// <c>[GenerateViewLocator]</c>. Each <c>XxxViewModel</c> is paired with an <c>XxxView</c> deriving from
/// <c>Avalonia.Controls.Control</c>, resolved by an explicit <c>[View]</c> attribute, then by naming
/// convention (same namespace, then a <c>ViewModels</c>→<c>Views</c> namespace), then by an assembly-wide
/// search.
/// </summary>
[Generator]
public sealed class ViewLocatorGenerator : IIncrementalGenerator
{
    private const string AttributeMetadataName = "CCSWE.Avalonia.ViewLocator.GenerateViewLocatorAttribute";
    private const string ControlMetadataName = "Avalonia.Controls.Control";
    private const string GlobalPrefix = "global::";
    private const string ViewAttributeMetadataName = "CCSWE.Avalonia.ViewLocator.ViewAttribute";
    private const string ViewModelSuffix = "ViewModel";
    private const string ViewModelsSegment = "ViewModels";
    private const string ViewSuffix = "View";
    private const string ViewsSegment = "Views";

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

        // Single pass: index every candidate view (Control-derived) by simple name for the assembly-wide
        // fallback, and collect candidate view models (suffix convention or an explicit [View]).
        var viewsByName = new Dictionary<string, List<INamedTypeSymbol>>();
        var viewModels = new List<INamedTypeSymbol>();

        foreach (var type in EnumerateTypes(compilation.Assembly.GlobalNamespace))
        {
            if (type.TypeKind != TypeKind.Class || !type.TypeParameters.IsEmpty)
            {
                continue;
            }

            if (InheritsOrEquals(type, controlSymbol))
            {
                if (!viewsByName.TryGetValue(type.Name, out var views))
                {
                    viewsByName[type.Name] = views = new List<INamedTypeSymbol>();
                }

                views.Add(type);
                continue;
            }

            if (type.IsAbstract || type.IsStatic
                || (viewModelBase is not null && !InheritsOrEquals(type, viewModelBase)))
            {
                continue;
            }

            if (HasExplicitView(type) || EndsWithViewModelSuffix(type))
            {
                viewModels.Add(type);
            }
        }

        var pairs = new List<(string ViewModel, string View)>();
        foreach (var viewModel in viewModels)
        {
            if (ResolveView(context, compilation, controlSymbol, viewsByName, viewModel) is { } view)
            {
                pairs.Add((viewModel.ToDisplayString(FullyQualified), view));
            }
        }

        if (pairs.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NoMappings, Location.None, target.ClassName));
        }

        pairs.Sort(static (a, b) => string.CompareOrdinal(a.ViewModel, b.ViewModel));

        context.AddSource($"{target.HintName}.ViewLocator.g.cs", SourceText.From(Emit(target, pairs), Encoding.UTF8));
    }

    private static bool EndsWithViewModelSuffix(INamedTypeSymbol type) =>
        type.Name.Length > ViewModelSuffix.Length && type.Name.EndsWith(ViewModelSuffix, StringComparison.Ordinal);

    private static bool HasExplicitView(INamedTypeSymbol type) => TryGetExplicitView(type, out _);

    private static INamedTypeSymbol? LookupView(
        Compilation compilation, INamedTypeSymbol controlSymbol, string? @namespace, string viewName)
    {
        var metadataName = string.IsNullOrEmpty(@namespace) ? viewName : @namespace + "." + viewName;

        return compilation.GetTypeByMetadataName(metadataName) is { } view && InheritsOrEquals(view, controlSymbol)
            ? view
            : null;
    }

    private static string? ResolveView(
        SourceProductionContext context,
        Compilation compilation,
        INamedTypeSymbol controlSymbol,
        Dictionary<string, List<INamedTypeSymbol>> viewsByName,
        INamedTypeSymbol viewModel)
    {
        // Tier 0 — explicit [View(typeof(...))] override; bypasses all conventions.
        if (TryGetExplicitView(viewModel, out var declared))
        {
            if (!InheritsOrEquals(declared!, controlSymbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.InvalidExplicitView, Location.None,
                    declared!.ToDisplayString(FullyQualified), viewModel.ToDisplayString(FullyQualified)));
                return null;
            }

            return declared!.ToDisplayString(FullyQualified);
        }

        if (!EndsWithViewModelSuffix(viewModel))
        {
            return null;
        }

        var viewName = viewModel.Name[..^ViewModelSuffix.Length] + ViewSuffix;
        var @namespace = viewModel.ContainingNamespace.IsGlobalNamespace
            ? null
            : viewModel.ContainingNamespace.ToDisplayString();

        // Tier 1 — same namespace; Tier 2 — ViewModels -> Views namespace.
        var view = LookupView(compilation, controlSymbol, @namespace, viewName);
        if (view is null && SwapViewModelsSegment(@namespace) is { } swapped)
        {
            view = LookupView(compilation, controlSymbol, swapped, viewName);
        }

        // Tier 3 — assembly-wide search by simple name; ambiguity is a diagnostic, never a guess.
        if (view is null && viewsByName.TryGetValue(viewName, out var candidates))
        {
            if (candidates.Count > 1)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.AmbiguousView, Location.None, viewName, viewModel.ToDisplayString(FullyQualified)));
                return null;
            }

            view = candidates[0];
        }

        return view?.ToDisplayString(FullyQualified);
    }

    private static string? SwapViewModelsSegment(string? @namespace)
    {
        if (string.IsNullOrEmpty(@namespace))
        {
            return null;
        }

        var segments = @namespace!.Split('.');
        var swapped = false;
        for (var i = 0; i < segments.Length; i++)
        {
            if (segments[i] == ViewModelsSegment)
            {
                segments[i] = ViewsSegment;
                swapped = true;
            }
        }

        return swapped ? string.Join(".", segments) : null;
    }

    private static bool TryGetExplicitView(INamedTypeSymbol type, out INamedTypeSymbol? viewType)
    {
        foreach (var attribute in type.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == ViewAttributeMetadataName
                && attribute.ConstructorArguments is [{ Value: INamedTypeSymbol declared }])
            {
                viewType = declared;
                return true;
            }
        }

        viewType = null;
        return false;
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
