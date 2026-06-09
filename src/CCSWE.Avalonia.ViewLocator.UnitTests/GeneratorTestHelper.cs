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
        AssertStepsAreCachedAcrossUnrelatedEdit(driver, compilation);

        return driver;
    }

    public static GeneratorDriver RunWithoutAvalonia(string source)
    {
        var references = References
            .Where(reference => !Path.GetFileName(reference.Display ?? string.Empty).StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var compilation = CSharpCompilation.Create(
            "Tests", [CSharpSyntaxTree.ParseText(source)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return CreateDriver().RunGenerators(compilation);
    }

    public static string SeverityOf(this GeneratorDriver driver, string id) =>
        driver.GetRunResult().Diagnostics.First(diagnostic => diagnostic.Id == id).Severity.ToString();

    private static void AssertGeneratedCodeCompiles(Compilation compilation) =>
        Assert.That(
            compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            Is.Empty,
            "Generated code should compile without errors.");

    private static void AssertStepsAreCachedAcrossUnrelatedEdit(GeneratorDriver driver, Compilation compilation)
    {
        var edited = compilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText("namespace Other { internal sealed class Unrelated { } }"));

        var trackedSteps = driver.RunGenerators(edited).GetRunResult().Results[0].TrackedSteps;

        foreach (var step in new[] { "ViewLocatorTargets", "ViewLocatorMappings" })
        {
            var reasons = trackedSteps[step].SelectMany(s => s.Outputs).Select(output => output.Reason);

            Assert.That(
                reasons,
                Has.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged),
                $"Step '{step}' should be cached across an unrelated edit.");
        }
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
