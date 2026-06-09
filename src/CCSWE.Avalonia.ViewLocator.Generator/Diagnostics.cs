using Microsoft.CodeAnalysis;

namespace CCSWE.Avalonia.ViewLocator.Generator;

internal static class Diagnostics
{
    private const string Category = "CCSWE.Avalonia.ViewLocator";

    public static readonly DiagnosticDescriptor AmbiguousView = new(
        "CAVL0004",
        "Ambiguous view match",
        "Multiple views named '{0}' were found for view model '{1}'; skipping — place the view in the view model's namespace or its 'Views' namespace to disambiguate",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AvaloniaNotReferenced = new(
        "CAVL0002",
        "Avalonia is not referenced",
        "Cannot generate a view locator: 'Avalonia.Controls.Control' was not found — is Avalonia referenced?",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidExplicitView = new(
        "CAVL0005",
        "Explicit view type is invalid",
        "View type '{0}' declared by [View] on '{1}' must be a non-abstract, non-generic type deriving from 'Avalonia.Controls.Control'",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoMappings = new(
        "CAVL0003",
        "No view-model/view pairs found",
        "No 'XxxViewModel' types with a matching 'XxxView' deriving from Control were found for '{0}'",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotPartial = new(
        "CAVL0001",
        "View locator class must be partial",
        "Class '{0}' marked with [GenerateViewLocator] must be partial",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedLocator = new(
        "CAVL0006",
        "View locator class is not supported",
        "Class '{0}' marked with [GenerateViewLocator] must be a non-generic, non-nested class",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
