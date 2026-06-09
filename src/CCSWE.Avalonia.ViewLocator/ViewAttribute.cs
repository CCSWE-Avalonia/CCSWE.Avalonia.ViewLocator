using JetBrains.Annotations;

namespace CCSWE.Avalonia.ViewLocator;

/// <summary>
/// Declares the view type for the annotated view model, overriding the locator's naming conventions. Use this
/// when a view model's view cannot be discovered by convention — for example a window named <c>MainWindow</c>
/// rather than <c>MainWindowView</c>. When present, the declared type is used with no convention or assembly
/// fallback; it must derive from <c>Avalonia.Controls.Control</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[PublicAPI]
public sealed class ViewAttribute : Attribute
{
    /// <summary>Creates the attribute mapping the annotated view model to <paramref name="viewType"/>.</summary>
    public ViewAttribute(Type viewType) => ViewType = viewType;

    /// <summary>The view type that the annotated view model resolves to.</summary>
    public Type ViewType { get; }
}
