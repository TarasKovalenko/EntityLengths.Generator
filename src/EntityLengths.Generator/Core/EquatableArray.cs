using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EntityLengths.Generator.Core;

/// <summary>
/// An immutable array with structural equality, so it can safely flow through the
/// incremental generator pipeline without breaking output caching.
/// </summary>
public readonly struct EquatableArray<T>(ImmutableArray<T> values)
    : IEquatable<EquatableArray<T>>,
        IReadOnlyList<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _values = values;

    public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

    private ImmutableArray<T> Values => _values.IsDefault ? ImmutableArray<T>.Empty : _values;

    public int Count => Values.Length;

    public T this[int index] => Values[index];

    public ImmutableArray<T> AsImmutableArray() => Values;

    public bool Equals(EquatableArray<T> other) => Values.SequenceEqual(other.Values);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var value in Values)
        {
            hash = (hash * 31) + (value?.GetHashCode() ?? 0);
        }

        return hash;
    }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal static class EquatableArrayExtensions
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> source)
        where T : IEquatable<T> => new(source.ToImmutableArray());
}
