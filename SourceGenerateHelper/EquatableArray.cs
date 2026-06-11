namespace SourceGenerateHelper;

using System.Collections.Generic;

public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
{
#pragma warning disable IDE0051
    public static readonly EquatableArray<T> Empty = new([]);
#pragma warning restore IDE0051

    private readonly T[]? array;

    public EquatableArray(T[] array)
    {
        this.array = array;
    }

    private T[] Values => array ?? [];

    public int Count => Values.Length;

    public T this[int index] => Values[index];

    public IEnumerator<T> GetEnumerator() => ((IReadOnlyList<T>)Values).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Values.GetEnumerator();

#pragma warning disable CA2225
    public static implicit operator T[](EquatableArray<T> value) => value.Values;

    public static implicit operator EquatableArray<T>(T[] value) => new(value);
#pragma warning restore CA2225

    public bool Equals(EquatableArray<T> other) => Values.SequenceEqual(other.Values);

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = 17;
        var comparer = EqualityComparer<T>.Default;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var item in Values)
        {
            hash = (hash * 31) + (item is null ? 0 : comparer.GetHashCode(item));
        }
        return hash;
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
