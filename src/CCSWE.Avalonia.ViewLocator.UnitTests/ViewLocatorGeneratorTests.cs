using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using NUnit.Framework;
using static VerifyNUnit.Verifier;

namespace CCSWE.Avalonia.ViewLocator.UnitTests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class ViewLocatorGeneratorTests
{
    public class When_Run_Is_Called : ViewLocatorGeneratorTests
    {
        [Test]
        public Task It_maps_a_same_namespace_view_model_to_its_view() =>
            Verify(GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel : ViewModelBase;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """));

        [Test]
        public Task It_uses_a_convention_match_when_no_base_type_is_given() =>
            Verify(GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public sealed class FooViewModel;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator]
                public partial class AppViewLocator;
                """));

        [Test]
        public void It_skips_a_view_model_without_a_matching_view()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class LonelyViewModel : ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(driver.GeneratedSource(), Does.Not.Contain("LonelyViewModel"));
            Assert.That(driver.DiagnosticIds(), Does.Contain("CCSWEVL003"));
        }

        [Test]
        public void It_skips_a_view_that_is_not_a_control()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel : ViewModelBase;
                public sealed class FooView;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(driver.GeneratedSource(), Does.Not.Contain("typeof(global::MyApp.FooView)"));
        }

        [Test]
        public void It_skips_a_generic_view_model()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel<T> : ViewModelBase;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(driver.GeneratedSource(), Does.Not.Contain("FooViewModel"));
        }

        [Test]
        public void It_reports_an_error_when_the_class_is_not_partial()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public class AppViewLocator;
                """);

            Assert.That(driver.DiagnosticIds(), Does.Contain("CCSWEVL001"));
        }
    }
}
