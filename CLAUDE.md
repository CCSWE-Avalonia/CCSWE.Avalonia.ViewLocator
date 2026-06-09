# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Project

`CCSWE.Avalonia.ViewLocator` is a compile-time, AOT/trim-safe Avalonia `ViewLocator`: a Roslyn source generator
maps `XxxViewModel` → `XxxView` by naming convention (explicit `[View]` override → same namespace →
`ViewModels`→`Views` → assembly-wide fallback) and resolves the view from `System.IServiceProvider`. See
`README.md` for usage. Targets `net10.0` / Avalonia 12.

## Architecture — runtime + bundled generator, one package

- **`CCSWE.Avalonia.ViewLocator`** (runtime, net10.0): the public `GenerateViewLocatorAttribute` and the static
  `ViewLocatorResolver` (the shared `Build` logic). References `Avalonia` only. This project IS the NuGet package
  and **bundles the generator dll as an analyzer** (`analyzers/dotnet/roslyn4.8/cs`).
- **`CCSWE.Avalonia.ViewLocator.Generator`** (netstandard2.0): the `IIncrementalGenerator`. Roslyn-only (no
  Avalonia reference — it checks for `Avalonia.Controls.Control` via the semantic model).

**No base class is imposed on the consumer.** The generator emits the entire `IDataTemplate` (interface + ctor +
`Build`/`Match`/`GetViewType`) into the consumer's `partial` class — adding only the interface, never a base
class — so the consumer keeps their inheritance slot. Shared logic lives in `ViewLocatorResolver` (a static), not
a base. The map is `typeof(...) == typeof(...)` (AOT/trim-safe); view resolution is `IServiceProvider.GetService`.

## Source-generator conventions (mirrors viceroypenguin's Immediate.*)

- Generator targets **netstandard2.0**, `IsRoslynComponent=true`, `EnforceExtendedAnalyzerRules=true`,
  `IncludeBuildOutput=false`, `IsPackable=false`. Pin `Microsoft.CodeAnalysis.CSharp` **4.8.0** (`PrivateAssets=all`)
  — building against newer Roslyn than the host IDE makes the analyzer silently not load; the
  `analyzers/dotnet/roslyn4.8/cs` pack folder matches the pin. `Microsoft.CodeAnalysis.Analyzers` lints the generator.
  `Meziantou.Polyfill` + `Microsoft.Bcl.HashCode` enable modern C# on netstandard2.0.
- Pipeline: `ForAttributeWithMetadataName` targets + a `CompilationProvider.Select` that resolves the assembly
  once into an equatable model (`record`s of `string`/`bool`/`EquatableReadOnlyList<T>`, never
  `ISymbol`/`Compilation`); `.WithTrackingName(...)` per step; emit with a `StringBuilder`.
- The runtime/package bundles the analyzer via `<ProjectReference … OutputItemType="Analyzer"
  ReferenceOutputAssembly="false" />` + a `<None Include="…Generator.dll" Pack="true"
  PackagePath="analyzers/dotnet/roslyn4.8/cs" />`.

## Build / pack / publish

```bash
dotnet build src/CCSWE.Avalonia.ViewLocator.slnx -c Release
dotnet test  src/CCSWE.Avalonia.ViewLocator.slnx
dotnet pack  src/CCSWE.Avalonia.ViewLocator.slnx -c Release -o artifacts
dotnet tool restore && dotnet validate package local artifacts/*.nupkg   # validate analyzer packaging
```

CPM (`src/Directory.Packages.props`); never put `Version=` on a `<PackageReference>`. Versioning via
Nerdbank.GitVersioning (`version.json`); major tracks the Avalonia major. CI builds/tests/packs/validates and
publishes to NuGet.org on `master`.

## Coding standards

Follow [standard C# conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

**Formatting.** 4-space indent, no tabs. Allman braces (each on its own line). Always brace control flow — never
omit braces for single-line bodies. One statement and one declaration per line. One blank line between members; no
consecutive blank lines. Space after flow-control keywords (`if (`, `for (`), none after method names (`Method(`).

**Naming.** `PascalCase` for types, methods, properties, constants, namespaces, public fields, and record primary
constructor parameters; `camelCase` for parameters and locals; `_camelCase` for private fields; `I`-prefix for
interfaces. Two-letter acronyms uppercase (`IO`), longer ones PascalCase (`Http`, `Json`).

**File organization.** One type per file, named `{TypeName}.cs`; partials as `{ClassName}.{Part}.cs`. File-scoped
namespaces aligned with folder structure. `using`s outside the namespace, ordered System → third-party → project
(global usings come from `ImplicitUsings`, enabled in `Directory.Build.props`).

**Access modifiers.** Always explicit. `internal` for implementation details; `[PublicAPI]` on the intentional
public runtime surface; `[ExcludeFromCodeCoverage]` on composition-only types.

**Language style.** `var` where the type is inferable; language keywords for built-in types (`string`, not
`String`); string interpolation over concatenation; `&&`/`||`, not `&`/`|`; `async`/`await` (never `.Result` /
`.Wait()`); expression-bodied members for single-line getters/methods; `nameof()` over string literals. Nullable
reference types are enabled project-wide — respect the annotations.

**XML docs.** Required on public/internal types and members in projects that set `GenerateDocumentationFile`;
`<inheritdoc />` when the interface doc suffices.

**Member order.** Group members as (1) constants / `static readonly` fields, (2) instance fields, (3) constructors,
(4) properties, (5) methods, and alphabetize strictly within each group **regardless of access modifier**. Nested
types go at the bottom of the file.

**Frozen collections.** Any never-mutated `static readonly` `HashSet<T>` / `Dictionary<,>` should be a
`FrozenSet<T>` / `FrozenDictionary<,>` (`System.Collections.Frozen`), built via `.ToFrozenSet(comparer)` /
`.ToFrozenDictionary()`.

## Testing

NUnit 4. Test project sits physically under `src/` (no on-disk `tests/` folder), shown in a `/tests/` solution
folder, named `<ProjectUnderTest>.UnitTests`. Follow AAA — separate the sections with blank lines, **not** with
`// Arrange` / `// Act` / `// Assert` comments.

- One outer class per type under test, `<ClassUnderTest>Tests`, decorated with
  `[SuppressMessage("ReSharper", "InconsistentNaming")]` and **not** `sealed` (nested classes inherit it).
- Nested classes group tests by method: `When_<MethodName>_Is_Called` (e.g. `When_Build_Is_Called`), inheriting the outer class.
- Test methods describe behavior with a lowercase `It_` prefix (e.g. `It_returns_the_resolved_view`).
- Generator tests go through `GeneratorTestHelper.Run`: runs the `CSharpGeneratorDriver`, asserts the generated
  code compiles, and asserts the `ViewLocatorTargets` and `ViewLocatorMappings` steps stay cached across an
  unrelated edit. Snapshot emitted source with **Verify** (`Verify.NUnit` + `Verify.SourceGenerators`); assert
  diagnostics inline by id (`driver.DiagnosticIds()`). `.verified.*` snapshots are committed under `Snapshots/`,
  `*.received.*` git-ignored; `AutoVerify(includeBuildServer: false)` auto-accepts locally but fails on CI.
- `Microsoft.CodeAnalysis.CSharp` is pinned to the **4.8** host floor; the test project bumps it via
  `VersionOverride` to exercise newer Roslyn. Verify.NUnit's implicit `using static VerifyNUnit.Verifier` is
  removed in the test csproj (it shadows NUnit's `Throws`).
- Coverage: `coverlet.collector` + `src/coverage.runsettings` (excludes test/generated code), ~99% on
  hand-written code.
