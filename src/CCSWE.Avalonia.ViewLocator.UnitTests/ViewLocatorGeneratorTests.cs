using System.Diagnostics.CodeAnalysis;
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
            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0003"));
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

            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0001"));
        }

        [Test]
        public Task It_maps_a_view_declared_by_the_view_attribute() =>
            Verify(GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                [View(typeof(ShellScreen))]
                public sealed class ShellViewModel : ViewModelBase;
                public sealed class ShellScreen : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """));

        [Test]
        public void It_lets_the_view_attribute_override_the_convention()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                [View(typeof(CustomView))]
                public sealed class FooViewModel : ViewModelBase;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                public sealed class CustomView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(driver.GeneratedSource(), Does.Contain("return typeof(global::MyApp.CustomView);"));
            Assert.That(driver.GeneratedSource(), Does.Not.Contain("typeof(global::MyApp.FooView);"));
        }

        [Test]
        public void It_reports_an_error_when_the_view_attribute_target_is_not_a_control()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class NotAControl;
                [View(typeof(NotAControl))]
                public sealed class FooViewModel : ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0005"));
        }

        [Test]
        public Task It_maps_a_view_model_to_a_view_in_the_views_namespace() =>
            Verify(GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp.ViewModels
                {
                    public abstract class ViewModelBase;
                    public sealed class FooViewModel : ViewModelBase;
                }
                namespace MyApp.Views
                {
                    public sealed class FooView : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp
                {
                    [GenerateViewLocator(typeof(global::MyApp.ViewModels.ViewModelBase))]
                    public partial class AppViewLocator;
                }
                """));

        [Test]
        public Task It_falls_back_to_an_assembly_wide_view_search() =>
            Verify(GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp.Features
                {
                    public sealed class WidgetViewModel;
                }
                namespace MyApp.Controls
                {
                    public sealed class WidgetView : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp
                {
                    [GenerateViewLocator]
                    public partial class AppViewLocator;
                }
                """));

        [Test]
        public void It_skips_an_ambiguous_assembly_wide_match()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp.Features
                {
                    public sealed class WidgetViewModel;
                }
                namespace MyApp.A
                {
                    public sealed class WidgetView : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp.B
                {
                    public sealed class WidgetView : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp
                {
                    [GenerateViewLocator]
                    public partial class AppViewLocator;
                }
                """);

            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0004"));
            Assert.That(driver.GeneratedSource(), Does.Not.Contain("typeof(global::MyApp.A.WidgetView)"));
        }

        [Test]
        public void It_prefers_the_same_namespace_view_over_an_assembly_wide_match()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp.Features
                {
                    public sealed class WidgetViewModel;
                    public sealed class WidgetView : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp.Other
                {
                    public sealed class WidgetView : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp
                {
                    [GenerateViewLocator]
                    public partial class AppViewLocator;
                }
                """);

            Assert.That(driver.GeneratedSource(), Does.Contain("return typeof(global::MyApp.Features.WidgetView);"));
            Assert.That(driver.DiagnosticIds(), Does.Not.Contain("CAVL0004"));
        }
    }
}
