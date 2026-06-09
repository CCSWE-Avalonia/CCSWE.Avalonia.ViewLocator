using CCSWE.Avalonia.ViewLocator.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CCSWE.Avalonia.ViewLocator.UnitTests;

internal static class GeneratorTestHelper
{
    private static readonly IReadOnlyList<MetadataReference> References = BuildReferences();

    public static IReadOnlyList<string> DiagnosticIds(this GeneratorDriver driver) =>
        driver.GetRunResult().Diagnostics.Select(diagnostic => diagnostic.Id).ToList();

    public static string GeneratedSource(this GeneratorDriver driver) =>
        driver.GetRunResult().GeneratedTrees.FirstOrDefault()?.ToString() ?? string.Empty;

    public static GeneratorDriver Run(string source)
    {
        var compilation = CreateCompilation(source);
        var driver = CreateDriver().RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        AssertGeneratedCodeCompiles(output);
        AssertTargetStepIsCachedAcrossUnrelatedEdit(driver, compilation);

        return driver;
    }

    private static void AssertGeneratedCodeCompiles(Compilation compilation) =>
        Assert.That(
            compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            Is.Empty,
            "Generated code should compile without errors.");

    private static void AssertTargetStepIsCachedAcrossUnrelatedEdit(GeneratorDriver driver, Compilation compilation)
    {
        var edited = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("namespace Other { internal sealed class Unrelated { } }"));

        var reasons = driver
            .RunGenerators(edited)
            .GetRunResult()
            .Results[0]
            .TrackedSteps["ViewLocatorTargets"]
            .SelectMany(step => step.Outputs)
            .Select(output => output.Reason);

        Assert.That(
            reasons,
            Has.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged),
            "Target step should be cached across an unrelated edit.");
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

    private static CSharpCompilation CreateCompilation(string source) =>
        CSharpCompilation.Create(
            "Tests",
            [CSharpSyntaxTree.ParseText(source)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static GeneratorDriver CreateDriver() =>
        CSharpGeneratorDriver.Create(
            generators: [new ViewLocatorGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));
}
