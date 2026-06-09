using System.Runtime.CompilerServices;

namespace CCSWE.Avalonia.ViewLocator.UnitTests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifySourceGenerators.Initialize();

        // Auto-accept snapshots locally — a changed/added .verified.* file in the diff is the review signal.
        // Disabled on build servers so CI still fails on any snapshot mismatch.
        VerifierSettings.AutoVerify(includeBuildServer: false);

        Verifier.DerivePathInfo((sourceFile, _, type, method) =>
            new PathInfo(
                directory: Path.Combine(Path.GetDirectoryName(sourceFile)!, "Snapshots"),
                typeName: type.Name,
                methodName: method.Name));
    }
}
