namespace SourceGenerateHelper.Tests;

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
        // Arrange
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 1, 2, 3 };

        // Act & Assert
        Assert.True(a.Equals(b));
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void NotEqualDifferentContent()
    {
        // Arrange
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 1, 2, 4 };

        // Act & Assert
        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void NotEqualDifferentLength()
    {
        // Arrange
        EquatableArray<int> a = new[] { 1, 2 };
        EquatableArray<int> b = new[] { 1, 2, 3 };

        // Act & Assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void NotEqualDifferentOrder()
    {
        // Arrange
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 3, 2, 1 };

        // Act & Assert
        Assert.False(a.Equals(b));
    }

    [Fact]
    public void EqualsBoxed()
    {
        // Arrange
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 1, 2, 3 };
        object boxed = b;

        // Act & Assert
        Assert.True(a.Equals(boxed));
    }

    [Fact]
    public void NotEqualsBoxedDifferent()
    {
        // Arrange
        EquatableArray<int> a = new[] { 1, 2, 3 };
        object boxed = "wrong";

        // Act & Assert
        Assert.False(a.Equals(boxed));
    }

    // ------------------------------------------------------------------
    // GetHashCode
    // ------------------------------------------------------------------

    [Fact]
    public void HashCodeEqualForEqualArrays()
    {
        // Arrange
        EquatableArray<int> a = new[] { 1, 2, 3 };
        EquatableArray<int> b = new[] { 1, 2, 3 };

        // Act & Assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void HashCodeDifferentForDifferentOrder()
    {
        // Arrange
        EquatableArray<int> a = new[] { 1, 2 };
        EquatableArray<int> b = new[] { 2, 1 };

        // Act & Assert
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    // ------------------------------------------------------------------
    // default (NRE 修正の検証)
    // ------------------------------------------------------------------

    [Fact]
    public void DefaultCountIsZero()
    {
        // Arrange
        var d = default(EquatableArray<int>);

        // Act & Assert
        Assert.True(d.Count == 0);
    }

    [Fact]
    public void DefaultEqualsEmpty()
    {
        // Arrange
        var d = default(EquatableArray<int>);

        // Act & Assert
        Assert.True(d == EquatableArray<int>.Empty);
        Assert.True(EquatableArray<int>.Empty.Equals(d));
    }

    [Fact]
    public void DefaultEqualsDefault()
    {
        // Arrange
        var d1 = default(EquatableArray<int>);
        var d2 = default(EquatableArray<int>);

        // Act & Assert
        Assert.True(d1 == d2);
    }

    [Fact]
    public void DefaultGetHashCodeDoesNotThrow()
    {
        // Arrange
        var d = default(EquatableArray<int>);

        // Act
        var hash = d.GetHashCode();

        // Assert
        Assert.Equal(EquatableArray<int>.Empty.GetHashCode(), hash);
    }

    [Fact]
    public void DefaultForeachNoElements()
    {
        // Arrange
        var d = default(EquatableArray<int>);

        // Act
        var count = d.Sum();

        // Assert
        Assert.True(count == 0);
    }

    [Fact]
    public void DefaultImplicitConversionToArrayIsNotNull()
    {
        // Arrange
        var d = default(EquatableArray<int>);

        // Act
        int[] arr = d;

        // Assert
        Assert.NotNull(arr);
        Assert.Empty(arr);
    }

    // ------------------------------------------------------------------
    // Indexer / Count / Enumeration
    // ------------------------------------------------------------------

    [Fact]
    public void IndexerAndCount()
    {
        // Arrange
        EquatableArray<int> arr = new[] { 10, 20, 30 };

        // Act & Assert
        Assert.True(arr.Count == 3);
        Assert.Equal(10, arr[0]);
        Assert.Equal(20, arr[1]);
        Assert.Equal(30, arr[2]);
    }

    [Fact]
    public void EnumerationContent()
    {
        // Arrange
        EquatableArray<int> arr = new[] { 10, 20, 30 };

        // Act
        var result = arr.ToList();

        // Assert
        Assert.Equal(Expected102030, result);
    }

    [Fact]
    public void NonGenericEnumerator()
    {
        // Arrange
        EquatableArray<int> arr = new[] { 1, 2 };
        var enumerable = (System.Collections.IEnumerable)arr;

        // Act
        var result = enumerable.Cast<int>().ToList();

        // Assert
        Assert.Equal(Expected12, result);
    }

    // ------------------------------------------------------------------
    // Implicit conversion T[] <-> EquatableArray<T>
    // ------------------------------------------------------------------

    [Fact]
    public void ImplicitConversionFromArray()
    {
        // Arrange
        var source = new[] { 1, 2, 3 };

        // Act
        EquatableArray<int> arr = source;

        // Assert
        Assert.True(arr.Count == 3);
        Assert.Equal(1, arr[0]);
    }

    [Fact]
    public void ImplicitConversionToArray()
    {
        // Arrange
        EquatableArray<int> arr = new[] { 1, 2, 3 };

        // Act
        int[] result = arr;

        // Assert
        Assert.Equal(Expected123, result);
    }

    // ------------------------------------------------------------------
    // Reference type with null elements
    // ------------------------------------------------------------------

    [Fact]
    public void NullElementsEqualityDoesNotThrow()
    {
        // Arrange
        EquatableArray<string?> a = new[] { null, "x", null };
        EquatableArray<string?> b = new[] { null, "x", null };

        // Act & Assert
        Assert.True(a == b);
    }

    [Fact]
    public void NullElementsHashCodeDoesNotThrow()
    {
        // Arrange
        EquatableArray<string?> a = new[] { null, "x", null };
        EquatableArray<string?> b = new[] { null, "x", null };

        // Act & Assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void NullElementsNotEqualDifferentContent()
    {
        // Arrange
        EquatableArray<string?> a = new[] { null, "x" };
        EquatableArray<string?> b = new[] { "x", null };

        // Act & Assert
        Assert.False(a == b);
    }
}
