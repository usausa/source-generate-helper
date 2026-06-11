namespace SourceGenerateHelper.Tests;

using System.Collections.Generic;

public sealed class EquatableArrayTest
{
    private static readonly int[] Expected102030 = [10, 20, 30];

    private static readonly int[] Expected12 = [1, 2];

    private static readonly int[] Expected123 = [1, 2, 3];

    // ------------------------------------------------------------------
    // Equality
    // ------------------------------------------------------------------

    [Fact]
    public void EqualsSameContent()
    {
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 1, 2, 3 };
        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void NotEqualDifferentContent()
    {
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 1, 2, 4 };
        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void NotEqualDifferentLength()
    {
        EquatableArray<int> a = new[] { 1, 2 };
        EquatableArray<int> b = new[] { 1, 2, 3 };
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEqualDifferentOrder()
    {
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 3, 2, 1 };
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void EqualsBoxed()
    {
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 1, 2, 3 };
        object boxed = b;
        Assert.True(a.Equals(boxed));
    }

    [Fact]
    public void NotEqualsBoxedDifferent()
    {
        EquatableArray<int> a = new[] { 1, 2, 3 };
        object boxed = "wrong";
        Assert.False(a.Equals(boxed));
    }

    // ------------------------------------------------------------------
    // GetHashCode
    // ------------------------------------------------------------------

    [Fact]
    public void HashCodeEqualForEqualArrays()
    {
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 1, 2, 3 };
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void HashCodeDifferentForDifferentOrder()
    {
        EquatableArray<int> a = new[] { 1, 2 };
        EquatableArray<int> b = new[] { 2, 1 };
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    // ------------------------------------------------------------------
    // default (NRE 修正の検証)
    // ------------------------------------------------------------------

    [Fact]
    public void DefaultCountIsZero()
    {
        var d = default(EquatableArray<int>);
        Assert.True(d.Count == 0);
    }

    [Fact]
    public void DefaultEqualsEmpty()
    {
        var d = default(EquatableArray<int>);
        Assert.True(d == EquatableArray<int>.Empty);
        Assert.True(EquatableArray<int>.Empty.Equals(d));
    }

    [Fact]
    public void DefaultEqualsDefault()
    {
        var d1 = default(EquatableArray<int>);
        var d2 = default(EquatableArray<int>);
        Assert.True(d1 == d2);
    }

    [Fact]
    public void DefaultGetHashCodeDoesNotThrow()
    {
        var d = default(EquatableArray<int>);
        var hash = d.GetHashCode();
        Assert.Equal(EquatableArray<int>.Empty.GetHashCode(), hash);
    }

    [Fact]
    public void DefaultForeachNoElements()
    {
        var d = default(EquatableArray<int>);
        var count = d.Sum();
        Assert.True(count == 0);
    }

    [Fact]
    public void DefaultImplicitConversionToArrayIsNotNull()
    {
        var d = default(EquatableArray<int>);
        int[] arr = d;
        Assert.NotNull(arr);
        Assert.Empty(arr);
    }

    // ------------------------------------------------------------------
    // Indexer / Count / Enumeration
    // ------------------------------------------------------------------

    [Fact]
    public void IndexerAndCount()
    {
        EquatableArray<int> arr = new[] { 10, 20, 30 };
        Assert.True(arr.Count == 3);
        Assert.Equal(10, arr[0]);
        Assert.Equal(20, arr[1]);
        Assert.Equal(30, arr[2]);
    }

    [Fact]
    public void EnumerationContent()
    {
        EquatableArray<int> arr = new[] { 10, 20, 30 };
        var result = arr.ToList();
        Assert.Equal(Expected102030, result);
    }

    [Fact]
    public void NonGenericEnumerator()
    {
        EquatableArray<int> arr = new[] { 1, 2 };
        var enumerable = (System.Collections.IEnumerable)arr;
        var result = enumerable.Cast<int>().ToList();
        Assert.Equal(Expected12, result);
    }

    // ------------------------------------------------------------------
    // Implicit conversion T[] <-> EquatableArray<T>
    // ------------------------------------------------------------------

    [Fact]
    public void ImplicitConversionFromArray()
    {
        var source = new[] { 1, 2, 3 };
        EquatableArray<int> arr = source;
        Assert.True(arr.Count == 3);
        Assert.Equal(1, arr[0]);
    }

    [Fact]
    public void ImplicitConversionToArray()
    {
        EquatableArray<int> arr = new[] { 1, 2, 3 };
        int[] result = arr;
        Assert.Equal(Expected123, result);
    }

    // ------------------------------------------------------------------
    // Reference type with null elements
    // ------------------------------------------------------------------

    [Fact]
    public void NullElementsEqualityDoesNotThrow()
    {
        EquatableArray<string?> a = new[] { null, "x", null };
        EquatableArray<string?> b = new[] { null, "x", null };
        Assert.True(a == b);
    }

    [Fact]
    public void NullElementsHashCodeDoesNotThrow()
    {
        EquatableArray<string?> a = new[] { null, "x", null };
        EquatableArray<string?> b = new[] { null, "x", null };
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NullElementsNotEqualDifferentContent()
    {
        EquatableArray<string?> a = new[] { null, "x" };
        EquatableArray<string?> b = new[] { "x", null };
        Assert.False(a == b);
    }
}
