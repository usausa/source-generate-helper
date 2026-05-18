namespace SourceGenerateHelper;

using System.Collections.Generic;

public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
{
    public static readonly EquatableArray<T> Empty = new([]);

    private readonly T[] array;

    public EquatableArray(T[] array)
    {
        this.array = array;
    }

    public T[] AsArray() => array;

#pragma warning disable CA2225
    public static implicit operator T[](EquatableArray<T> value) => value.array;

    public static implicit operator EquatableArray<T>(T[] value) => new(value);
#pragma warning restore CA2225

    public bool Equals(EquatableArray<T> other) => array.SequenceEqual(other.array);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 0;
        var comparer = EqualityComparer<T>.Default;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var item in array)
        {
            hash ^= item is null ? 0 : comparer.GetHashCode(item);
        }
        return hash;
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
