using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace CCSWE.Avalonia.ViewLocator.UnitTests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class ViewLocatorGeneratorTests
{
    public class When_Run_Is_Called : ViewLocatorGeneratorTests
    {
        [Test]
        public void It_maps_a_same_namespace_view_model_to_its_view()
        {
            var (source, diagnostics) = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel : ViewModelBase;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(diagnostics, Is.Empty);
            Assert.Multiple(() =>
            {
                Assert.That(source, Does.Contain("partial class AppViewLocator : global::Avalonia.Controls.Templates.IDataTemplate"));
                Assert.That(source, Does.Contain("if (viewModelType == typeof(global::MyApp.FooViewModel))"));
                Assert.That(source, Does.Contain("return typeof(global::MyApp.FooView);"));
                Assert.That(source, Does.Contain("public bool Match(object? data) => data is global::MyApp.ViewModelBase;"));
            });
        }

        [Test]
        public void It_uses_a_convention_match_when_no_base_type_is_given()
        {
            var (source, _) = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public sealed class FooViewModel;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator]
                public partial class AppViewLocator;
                """);

            Assert.That(source, Does.Contain("return typeof(global::MyApp.FooView);"));
            Assert.That(source, Does.Contain("data is not null && GetViewType(data.GetType()) is not null"));
        }

        [Test]
        public void It_skips_a_view_model_without_a_matching_view()
        {
            var (source, diagnostics) = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class LonelyViewModel : ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(source, Does.Not.Contain("LonelyViewModel"));
            Assert.That(diagnostics.Select(d => d.Id), Does.Contain("CCSWEVL003"));
        }

        [Test]
        public void It_skips_a_view_that_is_not_a_control()
        {
            var (source, _) = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel : ViewModelBase;
                public sealed class FooView;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(source, Does.Not.Contain("typeof(global::MyApp.FooView)"));
        }

        [Test]
        public void It_skips_a_generic_view_model()
        {
            var (source, _) = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel<T> : ViewModelBase;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(source, Does.Not.Contain("FooViewModel"));
        }

        [Test]
        public void It_reports_an_error_when_the_class_is_not_partial()
        {
            var (_, diagnostics) = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public class AppViewLocator;
                """);

            Assert.That(diagnostics.Select(d => d.Id), Does.Contain("CCSWEVL001"));
        }

        [Test]
        public void It_caches_the_target_step_across_an_unrelated_edit()
        {
            const string source =
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel : ViewModelBase;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """;

            var compilation = GeneratorTestHelper.CreateCompilation(source);
            var driver = GeneratorTestHelper.CreateDriver(trackSteps: true).RunGenerators(compilation);

            var edited = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("namespace Other { internal sealed class Unrelated { } }"));
            driver = driver.RunGenerators(edited);

            var steps = driver.GetRunResult().Results[0].TrackedSteps["ViewLocatorTargets"];
            Assert.That(
                steps.SelectMany(step => step.Outputs).Select(output => output.Reason),
                Has.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged));
        }
    }
}
