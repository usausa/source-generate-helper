namespace SourceGenerateHelper;

using System.Collections.Generic;

public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
{
#pragma warning disable IDE0051
    public static readonly EquatableArray<T> Empty = new([]);
#pragma warning restore IDE0051

    private readonly T[] array;

    public EquatableArray(T[] array)
    {
        this.array = array;
    }

    public Span<T> AsSpan() => array;

    public T[] AsArray() => array;

    public int Count => array.Length;

    public T this[int index] => array[index];

    public IEnumerator<T> GetEnumerator() => ((IReadOnlyList<T>)array).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => array.GetEnumerator();

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
