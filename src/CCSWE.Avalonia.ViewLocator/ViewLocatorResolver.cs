using System;
using Avalonia.Controls;
using JetBrains.Annotations;

namespace CCSWE.Avalonia.ViewLocator;

/// <summary>
/// Shared runtime logic for generated view locators: resolves the view for a view-model object from an
/// <see cref="IServiceProvider"/>. Invoked by the code the <see cref="GenerateViewLocatorAttribute"/> generator
/// emits; not typically called directly.
/// </summary>
[PublicAPI]
public static class ViewLocatorResolver
{
    /// <summary>
    /// Returns the view <see cref="Control"/> for <paramref name="data"/>, or a diagnostic placeholder when no
    /// view is mapped.
    /// </summary>
    /// <param name="data">The view-model instance, or <see langword="null"/>.</param>
    /// <param name="services">The provider used to construct the view.</param>
    /// <param name="getViewType">Maps a view-model <see cref="Type"/> to its view <see cref="Type"/>, or <see langword="null"/>.</param>
    /// <exception cref="InvalidOperationException">The mapped view is not registered, or is not a <see cref="Control"/>.</exception>
    public static Control? Build(object? data, IServiceProvider services, Func<Type, Type?> getViewType)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(getViewType);

        if (data is null)
        {
            return null;
        }

        var viewType = getViewType(data.GetType());
        if (viewType is null)
        {
            return new TextBlock { Text = "View not found for view model: " + data.GetType().FullName };
        }

        var view = services.GetService(viewType);
        if (view is null)
        {
            throw new InvalidOperationException(
                $"No service is registered for view type '{viewType.FullName}' (view model '{data.GetType().FullName}'). "
                + $"Register the view with your container, e.g. services.AddTransient<{viewType.Name}>().");
        }

        if (view is not Control control)
        {
            throw new InvalidOperationException(
                $"The service for view type '{viewType.FullName}' is not an Avalonia Control (actual type '{view.GetType().FullName}').");
        }

        return control;
    }
}
