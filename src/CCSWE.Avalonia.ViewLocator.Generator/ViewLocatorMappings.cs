namespace CCSWE.Avalonia.ViewLocator.Generator;

/// <summary>
/// The equatable, assembly-wide view-model → view resolution computed once per compilation and shared across
/// every <c>[GenerateViewLocator]</c> target. Carries only strings/bools/equatable lists so the pipeline caches.
/// </summary>
internal sealed record ViewLocatorMappings
{
    public required bool AvaloniaReferenced { get; init; }

    public required EquatableReadOnlyList<ViewModelMapping> ViewModels { get; init; }
}

/// <summary>One candidate view model: its resolved view (if any), its base-type chain, or a resolution diagnostic.</summary>
internal sealed record ViewModelMapping
{
    public required EquatableReadOnlyList<string> BaseChain { get; init; }

    public required ResolutionDiagnostic? Diagnostic { get; init; }

    public required string ViewModelFullName { get; init; }

    public required string? ViewFullName { get; init; }
}

internal enum ResolutionDiagnosticKind
{
    AmbiguousView,
    InvalidExplicitView,
}

/// <summary>A per-view-model resolution diagnostic, reported by each target that includes the view model.</summary>
internal sealed record ResolutionDiagnostic
{
    public required EquatableReadOnlyList<string> Arguments { get; init; }

    public required ResolutionDiagnosticKind Kind { get; init; }
}
