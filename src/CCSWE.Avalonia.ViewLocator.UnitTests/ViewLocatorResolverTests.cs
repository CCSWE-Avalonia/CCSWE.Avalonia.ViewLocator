using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using NUnit.Framework;

namespace CCSWE.Avalonia.ViewLocator.UnitTests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class ViewLocatorResolverTests
{
    public class When_Build_Is_Called : ViewLocatorResolverTests
    {
        [Test]
        public void It_returns_null_for_null_data()
        {
            var result = ViewLocatorResolver.Build(null, new StubProvider(null, null), _ => typeof(StubView));

            Assert.That(result, Is.Null);
        }

        [Test]
        public void It_returns_the_resolved_view()
        {
            var view = new StubView();

            var result = ViewLocatorResolver.Build(new object(), new StubProvider(typeof(StubView), view), _ => typeof(StubView));

            Assert.That(result, Is.SameAs(view));
        }

        [Test]
        public void It_returns_a_not_found_placeholder_when_unmapped()
        {
            var result = ViewLocatorResolver.Build(new object(), new StubProvider(null, null), _ => null);

            Assert.That(result, Is.TypeOf<TextBlock>());
            Assert.That(((TextBlock) result!).Text, Does.Contain("View not found"));
        }

        [Test]
        public void It_throws_when_the_view_is_not_registered()
        {
            Assert.That(
                () => ViewLocatorResolver.Build(new object(), new StubProvider(null, null), _ => typeof(StubView)),
                Throws.InvalidOperationException);
        }

        [Test]
        public void It_throws_when_the_service_is_not_a_control()
        {
            Assert.That(
                () => ViewLocatorResolver.Build(new object(), new StubProvider(typeof(StubView), "not a control"), _ => typeof(StubView)),
                Throws.InvalidOperationException);
        }
    }

    private sealed class StubProvider(Type? serviceType, object? instance) : IServiceProvider
    {
        public object? GetService(Type type) => type == serviceType ? instance : null;
    }

    private sealed class StubView : Control;
}
