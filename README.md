# CCSWE.Avalonia.ViewLocator

[![Build](https://img.shields.io/github/actions/workflow/status/CCSWE-Avalonia/CCSWE.Avalonia.ViewLocator/dotnet-build-publish-library.yml?branch=master&label=build)](https://github.com/CCSWE-Avalonia/CCSWE.Avalonia.ViewLocator/actions/workflows/dotnet-build-publish-library.yml)
[![NuGet](https://img.shields.io/nuget/v/CCSWE.Avalonia.ViewLocator.svg)](https://www.nuget.org/packages/CCSWE.Avalonia.ViewLocator)
[![Downloads](https://img.shields.io/nuget/dt/CCSWE.Avalonia.ViewLocator.svg)](https://www.nuget.org/packages/CCSWE.Avalonia.ViewLocator)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.md)

A **compile-time, AOT/trim-safe Avalonia `ViewLocator`**. A Roslyn source generator maps each
`XxxViewModel` to its `XxxView` **by same-namespace naming convention** and resolves the view from your
`IServiceProvider` — no reflection, and no hand-maintained `Type → Type` map.

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
- **No hand-maintained map** — add a `ViewModel`/`View` pair in the same folder and it's wired up automatically.
- **Feature-first friendly** — maps within a **single namespace** (`MyApp.Emulators.{EmulatorViewModel,EmulatorView}`),
  not a `ViewModels`→`Views` split.
- **DI-resolved views** — uses only `System.IServiceProvider` (no `Microsoft.Extensions.DependencyInjection` dependency).

## Convention

A class named `XxxViewModel` maps to `XxxView` **in the same namespace**, when that `XxxView` exists and derives
from `Avalonia.Controls.Control`. Supplying a base type — `[GenerateViewLocator(typeof(ViewModelBase))]` — scopes
discovery to view models assignable to it and makes `Match` return `data is ViewModelBase`.

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
