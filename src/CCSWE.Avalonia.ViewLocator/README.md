# CCSWE.Avalonia.ViewLocator

A compile-time, AOT/trim-safe Avalonia `ViewLocator`. A Roslyn source generator pairs each `FooViewModel` with its
`FooView` **by naming convention** (same namespace, a `ViewModels`→`Views` namespace, or an assembly-wide search)
— with a `[View]` override — and resolves the view from your `IServiceProvider` — no reflection, no
hand-maintained map.

```csharp
[GenerateViewLocator(typeof(ViewModelBase))]
public partial class ViewLocator;
```

The generator emits the entire `IDataTemplate` (ctor, `Build`, `Match`, and the view-model → view map) into the
partial — adding only the interface, never a base class. Register it like any data template:

```csharp
DataTemplates.Add(new ViewLocator(serviceProvider));
```

Views are resolved via `IServiceProvider.GetService(viewType)`, so register each view with your container
(e.g. `services.AddTransient<EmulatorView>()`).

See the [repository](https://github.com/CCSWE-Avalonia/CCSWE.Avalonia.ViewLocator) for full usage and the
convention details.

MIT © Cory Charlton / CCSWE.
