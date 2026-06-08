using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCSWE.Avalonia.ViewLocator.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CCSWE.Avalonia.ViewLocator.UnitTests;

internal static class GeneratorTestHelper
{
    private static readonly IReadOnlyList<MetadataReference> References = BuildReferences();

    public static GeneratorDriver CreateDriver(bool trackSteps = false) =>
        CSharpGeneratorDriver.Create(
            generators: [new ViewLocatorGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: trackSteps));

    public static CSharpCompilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "Tests",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    public static (string Source, IReadOnlyList<Diagnostic> Diagnostics) Run(string source)
    {
        var result = CreateDriver().RunGenerators(CreateCompilation(source)).GetRunResult();
        var tree = result.GeneratedTrees.FirstOrDefault();
        return (tree?.ToString() ?? string.Empty, result.Diagnostics);
    }

    private static IReadOnlyList<MetadataReference> BuildReferences()
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
        {
            foreach (var path in trusted.Split(Path.PathSeparator).Where(p => !string.IsNullOrEmpty(p)))
            {
                paths[Path.GetFileNameWithoutExtension(path)] = path;
            }
        }

        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "*.dll"))
        {
            paths[Path.GetFileNameWithoutExtension(dll)] = dll;
        }

        return paths.Values.Select(path => (MetadataReference) MetadataReference.CreateFromFile(path)).ToList();
    }
}
