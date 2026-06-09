# CCSWE.Avalonia.ViewLocator

[![Build](https://img.shields.io/github/actions/workflow/status/CCSWE-Avalonia/CCSWE.Avalonia.ViewLocator/dotnet-build-publish-library.yml?branch=master&label=build)](https://github.com/CCSWE-Avalonia/CCSWE.Avalonia.ViewLocator/actions/workflows/dotnet-build-publish-library.yml)
[![NuGet](https://img.shields.io/nuget/v/CCSWE.Avalonia.ViewLocator.svg)](https://www.nuget.org/packages/CCSWE.Avalonia.ViewLocator)
[![Downloads](https://img.shields.io/nuget/dt/CCSWE.Avalonia.ViewLocator.svg)](https://www.nuget.org/packages/CCSWE.Avalonia.ViewLocator)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

A **compile-time, AOT/trim-safe Avalonia `ViewLocator`**. A Roslyn source generator pairs each
`XxxViewModel` with its `XxxView` **by naming convention** — with an assembly-wide fallback and an explicit
`[View]` override — and resolves the view from your `IServiceProvider` — no reflection, and no hand-maintained
`Type → Type` map.

```csharp
[GenerateViewLocator(typeof(ViewModelBase))]
public partial class ViewLocator;
```

That's the whole locator. The generator emits the `IDataTemplate` implementation (the constructor, `Build`,
`Match`, and the view-model → view map) into the partial class — it adds only the **interface**, never a base
class, so your `ViewLocator` is free to derive from anything. Register it as you would any data template:

```csharp
DataTemplates.Add(new ViewLocator(serviceProvider));
```

Views are resolved from the service provider (`IServiceProvider.GetService(viewType)`), so register each view
with your container (e.g. `services.AddTransient<EmulatorView>()`) and they stay constructor-injectable.

## Why

- **No reflection / AOT- and trim-safe** — the map is plain `typeof(...) == typeof(...)`, generated at build time.
- **No hand-maintained map** — add a matching `XxxViewModel`/`XxxView` pair and it's wired up automatically.
- **Any project layout** — vertical-slice (`MyApp.Emulators.{EmulatorViewModel,EmulatorView}`) *and* the
  clean-architecture `ViewModels`→`Views` split both work, with an assembly-wide fallback and a `[View]`
  override for everything else.
- **DI-resolved views** — uses only `System.IServiceProvider` (no `Microsoft.Extensions.DependencyInjection` dependency).

## Convention

For each `XxxViewModel`, the generator finds a concrete `XxxView` deriving from `Avalonia.Controls.Control`,
resolved in this order — first match wins:

1. **Explicit override** — `[View(typeof(XxxView))]` on the view model maps it directly, bypassing all
   conventions. Use it for pairs that don't follow a naming convention — e.g. a `MainWindowViewModel` whose view
   is `MainWindow`. The declared type must be a concrete (non-abstract) class deriving from `Control`.
2. **Same namespace** — `XxxView` alongside `XxxViewModel` (vertical slices).
3. **`ViewModels`→`Views`** — `XxxView` in the sibling namespace formed by replacing a `ViewModels` segment with
   `Views` (the standard Avalonia MVVM layout).
4. **Assembly-wide** — any `XxxView` deriving from `Control` anywhere in the assembly. If more than one matches,
   the pair is skipped with a warning rather than guessed.

Abstract and open-generic views are ignored (they can't be instantiated). The locator class itself must be a
non-generic, top-level `partial` class.

Supplying a base type — `[GenerateViewLocator(typeof(ViewModelBase))]` — scopes discovery to view models
assignable to it; `Match` then claims only `ViewModelBase` instances that actually resolve to a view, so it never
claims data it can't build. Without a base type, every `XxxViewModel` is considered.

## Install

```
dotnet add package CCSWE.Avalonia.ViewLocator
```

## Build & run

```bash
dotnet build src/CCSWE.Avalonia.ViewLocator.slnx -c Release
dotnet test  src/CCSWE.Avalonia.ViewLocator.slnx
dotnet run --project src/CCSWE.Avalonia.ViewLocator.Sample
dotnet pack  src/CCSWE.Avalonia.ViewLocator.slnx -c Release -o artifacts
```

The major version tracks the Avalonia major (`12.x` → Avalonia 12.x), via Nerdbank.GitVersioning.

## Acknowledgments

The source-generator structure and incremental-pipeline patterns in this library were learned from
[**viceroypenguin** (Stuart Turner)](https://github.com/viceroypenguin)'s excellent
[`Immediate.Apis`](https://github.com/viceroypenguin/Immediate.Apis) and
[`Immediate.Handlers`](https://github.com/viceroypenguin/Immediate.Handlers) projects (MIT). Thank you for the
clear, production-hardened reference.

## License

[MIT](LICENSE.md) © Cory Charlton / CCSWE.
