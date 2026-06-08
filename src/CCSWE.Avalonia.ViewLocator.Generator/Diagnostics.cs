using Microsoft.CodeAnalysis;

namespace CCSWE.Avalonia.ViewLocator.Generator;

internal static class Diagnostics
{
    private const string Category = "CCSWE.Avalonia.ViewLocator";

    public static readonly DiagnosticDescriptor AvaloniaNotReferenced = new(
        "CCSWEVL002",
        "Avalonia is not referenced",
        "Cannot generate a view locator: 'Avalonia.Controls.Control' was not found — is Avalonia referenced?",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoMappings = new(
        "CCSWEVL003",
        "No view-model/view pairs found",
        "No 'XxxViewModel' types with a same-namespace 'XxxView' deriving from Control were found for '{0}'",
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NotPartial = new(
        "CCSWEVL001",
        "View locator class must be partial",
        "Class '{0}' marked with [GenerateViewLocator] must be partial",
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
