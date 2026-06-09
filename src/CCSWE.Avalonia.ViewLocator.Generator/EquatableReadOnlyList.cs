using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CCSWE.Avalonia.ViewLocator.Generator;

internal static class EquatableReadOnlyList
{
    public static EquatableReadOnlyList<T> ToEquatableReadOnlyList<T>(this IEnumerable<T> source) =>
        new(source as IReadOnlyList<T> ?? source.ToList());
}

/// <summary>
/// Wraps an <see cref="IReadOnlyList{T}"/> with value (sequence) equality so it can be carried in the equatable
/// models that flow through the incremental generator pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
internal readonly struct EquatableReadOnlyList<T>(IReadOnlyList<T>? items) : IEquatable<EquatableReadOnlyList<T>>, IReadOnlyList<T>
{
    private IReadOnlyList<T> Items => items ?? [];

    public int Count => Items.Count;

    public T this[int index] => Items[index];

    public bool Equals(EquatableReadOnlyList<T> other) => this.SequenceEqual(other);

    public override bool Equals(object? obj) => obj is EquatableReadOnlyList<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var item in Items)
        {
            hash.Add(item);
        }

        return hash.ToHashCode();
    }

    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

    public static bool operator ==(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right) => left.Equals(right);

    public static bool operator !=(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right) => !left.Equals(right);
}
