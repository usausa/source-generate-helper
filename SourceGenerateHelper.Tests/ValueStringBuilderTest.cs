namespace SourceGenerateHelper.Tests;

public sealed class ValueStringBuilderTest
{
    [Fact]
    public void AppendWithinInitialBuffer()
    {
        // Arrange
        Span<char> buffer = stackalloc char[16];
        var builder = new ValueStringBuilder(buffer);

        // Act
        builder.Append("abc");
        var result = builder.ToString();
        builder.Dispose();

        // Assert
        Assert.Equal("abc", result);
    }

    [Fact]
    public void AppendStringGrowsBeyondInitialBuffer()
    {
        // Arrange
        Span<char> buffer = stackalloc char[4];
        var builder = new ValueStringBuilder(buffer);

        // Act
        builder.Append("0123456789");
        var result = builder.ToString();
        builder.Dispose();

        // Assert
        Assert.Equal("0123456789", result);
    }

    [Fact]
    public void AppendSpanGrows()
    {
        // Arrange
        Span<char> buffer = stackalloc char[2];
        var builder = new ValueStringBuilder(buffer);

        // Act
        builder.Append("ab".AsSpan());
        builder.Append("cdef".AsSpan());
        var result = builder.ToString();
        builder.Dispose();

        // Assert
        Assert.Equal("abcdef", result);
    }

    [Fact]
    public void AppendCharGrows()
    {
        // Arrange
        Span<char> buffer = stackalloc char[2];
        var builder = new ValueStringBuilder(buffer);

        // Act
        builder.Append('a');
        builder.Append('b');
        builder.Append('c');
        var result = builder.ToString();
        builder.Dispose();

        // Assert
        Assert.Equal("abc", result);
    }

    [Fact]
    public void ToTrimStringTrimsWhitespace()
    {
        // Arrange
        Span<char> buffer = stackalloc char[16];
        var builder = new ValueStringBuilder(buffer);

        // Act
        builder.Append("  ab  ");
        var result = builder.ToTrimString();
        builder.Dispose();

        // Assert
        Assert.Equal("ab", result);
    }

    [Fact]
    public void DisposeMultipleTimesDoesNotThrow()
    {
        // Arrange
        Span<char> buffer = stackalloc char[4];
        var builder = new ValueStringBuilder(buffer);
        builder.Append("0123456789");

        // Act & Assert
        builder.Dispose();
        builder.Dispose();
    }
}
