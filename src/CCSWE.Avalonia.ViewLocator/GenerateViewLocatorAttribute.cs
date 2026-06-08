using System;
using JetBrains.Annotations;

namespace CCSWE.Avalonia.ViewLocator;

/// <summary>
/// Marks a partial class for which the view-locator source generator emits an Avalonia <c>IDataTemplate</c>
/// implementation mapping <c>XxxViewModel</c> to <c>XxxView</c> by same-namespace naming convention.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[PublicAPI]
public sealed class GenerateViewLocatorAttribute : Attribute
{
    /// <summary>Creates the attribute with no base-type scoping (convention-only discovery).</summary>
    public GenerateViewLocatorAttribute()
    {
    }

    /// <summary>
    /// Creates the attribute scoping discovery to view models assignable to <paramref name="viewModelBaseType"/>;
    /// the generated <c>Match</c> returns whether the data is an instance of that type.
    /// </summary>
    public GenerateViewLocatorAttribute(Type viewModelBaseType) => ViewModelBaseType = viewModelBaseType;

    /// <summary>The base view-model type that scopes discovery and drives <c>Match</c>, or <see langword="null"/>.</summary>
    public Type? ViewModelBaseType { get; }
}
