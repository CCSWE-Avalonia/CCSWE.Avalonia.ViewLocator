namespace CCSWE.Avalonia.ViewLocator.Generator;

/// <summary>
/// The equatable model for a <c>[GenerateViewLocator]</c> class flowing through the generator pipeline.
/// Holds only strings/bools (never symbols) so pipeline nodes cache correctly.
/// </summary>
internal sealed record ViewLocatorTarget
{
    public required string ClassName { get; init; }

    public required string HintName { get; init; }

    public required bool IsGeneric { get; init; }

    public required bool IsNested { get; init; }

    public required bool IsPartial { get; init; }

    public required string? Namespace { get; init; }

    public required string? ViewModelBaseFullyQualified { get; init; }
}
