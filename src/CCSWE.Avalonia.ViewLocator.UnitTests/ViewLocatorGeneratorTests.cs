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

        [Test]
        public void It_skips_an_abstract_view()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel : ViewModelBase;
                public abstract class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(driver.GeneratedSource(), Does.Not.Contain("typeof(global::MyApp.FooView)"));
            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0003"));
        }

        [Test]
        public void It_reports_an_error_when_the_view_attribute_target_is_abstract()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public abstract class FooView : global::Avalonia.Controls.UserControl;
                [View(typeof(FooView))]
                public sealed class FooViewModel : ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0005"));
        }

        [Test]
        public void It_reports_an_error_when_the_locator_class_is_nested()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public partial class Outer
                {
                    [GenerateViewLocator(typeof(ViewModelBase))]
                    public partial class AppViewLocator;
                }
                """);

            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0006"));
        }

        [Test]
        public Task It_maps_multiple_view_models_in_sorted_order() =>
            Verify(GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class ZebraViewModel : ViewModelBase;
                public sealed class ZebraView : global::Avalonia.Controls.UserControl;
                public sealed class AlphaViewModel : ViewModelBase;
                public sealed class AlphaView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """));

        [Test]
        public Task It_generates_independent_locators_for_multiple_targets() =>
            Verify(GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class FooViewModel : ViewModelBase;
                public sealed class FooView : global::Avalonia.Controls.UserControl;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class FirstViewLocator;
                [GenerateViewLocator]
                public partial class SecondViewLocator;
                """));

        [Test]
        public void It_reports_diagnostics_at_the_expected_severity()
        {
            var notPartial = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public class AppViewLocator;
                """);

            var noMappings = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                public sealed class LonelyViewModel : ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.Multiple(() =>
            {
                Assert.That(notPartial.SeverityOf("CAVL0001"), Is.EqualTo("Error"));
                Assert.That(noMappings.SeverityOf("CAVL0003"), Is.EqualTo("Warning"));
            });
        }

        [Test]
        public void It_reports_an_error_when_avalonia_is_not_referenced()
        {
            var driver = GeneratorTestHelper.RunWithoutAvalonia(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator;
                """);

            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0002"));
        }

        [Test]
        public void It_prefers_the_views_namespace_over_an_assembly_wide_match()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp.ViewModels
                {
                    public abstract class ViewModelBase;
                    public sealed class ItemViewModel : ViewModelBase;
                }
                namespace MyApp.Views
                {
                    public sealed class ItemView : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp.Other
                {
                    public sealed class ItemView : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp
                {
                    [GenerateViewLocator(typeof(global::MyApp.ViewModels.ViewModelBase))]
                    public partial class AppViewLocator;
                }
                """);

            Assert.That(driver.GeneratedSource(), Does.Contain("return typeof(global::MyApp.Views.ItemView);"));
            Assert.That(driver.DiagnosticIds(), Does.Not.Contain("CAVL0004"));
        }

        [Test]
        public void It_honors_the_view_attribute_across_namespaces_and_without_a_suffix()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp.Screens
                {
                    public sealed class Dashboard : global::Avalonia.Controls.UserControl;
                }
                namespace MyApp
                {
                    public abstract class ViewModelBase;
                    [View(typeof(global::MyApp.Screens.Dashboard))]
                    public sealed class Home : ViewModelBase;
                    [GenerateViewLocator(typeof(ViewModelBase))]
                    public partial class AppViewLocator;
                }
                """);

            Assert.Multiple(() =>
            {
                Assert.That(driver.GeneratedSource(), Does.Contain("if (viewModelType == typeof(global::MyApp.Home))"));
                Assert.That(driver.GeneratedSource(), Does.Contain("return typeof(global::MyApp.Screens.Dashboard);"));
            });
        }

        [Test]
        public void It_reports_an_error_when_the_locator_class_is_generic()
        {
            var driver = GeneratorTestHelper.Run(
                """
                using CCSWE.Avalonia.ViewLocator;
                namespace MyApp;
                public abstract class ViewModelBase;
                [GenerateViewLocator(typeof(ViewModelBase))]
                public partial class AppViewLocator<T>;
                """);

            Assert.That(driver.DiagnosticIds(), Does.Contain("CAVL0006"));
        }
    }
}
