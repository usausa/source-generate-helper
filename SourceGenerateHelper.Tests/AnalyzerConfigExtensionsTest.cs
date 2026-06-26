namespace SourceGenerateHelper.Tests;

using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis.Diagnostics;

public sealed class AnalyzerConfigExtensionsTest
{
    private enum Color
    {
        Red,
        Green,
        Blue
    }

    private sealed class FakeOptions(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
        {
            if (values.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }

            value = null;
            return false;
        }
    }

    private static FakeOptions Options(string key, string value) =>
        new(new Dictionary<string, string> { [$"build_property.{key}"] = value });

    // ------------------------------------------------------------------
    // GetValue
    // ------------------------------------------------------------------

    [Fact]
    public void GetValueString()
    {
        Assert.Equal("abc", Options("Key", "abc").GetValue<string>("Key"));
    }

    [Fact]
    public void GetValueInt()
    {
        Assert.Equal(123, Options("Key", "123").GetValue<int>("Key"));
    }

    [Fact]
    public void GetValueBool()
    {
        Assert.True(Options("Key", "true").GetValue<bool>("Key"));
    }

    [Fact]
    public void GetValueEnum()
    {
        Assert.Equal(Color.Blue, Options("Key", "Blue").GetValue<Color>("Key"));
    }

    [Fact]
    public void GetValueEnumIgnoreCase()
    {
        Assert.Equal(Color.Green, Options("Key", "green").GetValue<Color>("Key"));
    }

    [Fact]
    public void GetValueNullableIntWithValue()
    {
        var value = Options("Key", "5").GetValue<int?>("Key");

        Assert.Equal(5, value);
    }

    [Fact]
    public void GetValueNullableIntMissingKeyReturnsNull()
    {
        var value = Options("Key", "5").GetValue<int?>("Other");

        Assert.False(value.HasValue);
    }

    [Fact]
    public void GetValueEmptyStringNonStringReturnsDefault()
    {
        Assert.Equal(0, Options("Key", String.Empty).GetValue<int>("Key"));
    }

    [Fact]
    public void GetValueMissingKeyReturnsDefault()
    {
        Assert.Equal(0, Options("Key", "1").GetValue<int>("Other"));
    }

    [Fact]
    public void GetValueInvalidThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Options("Key", "abc").GetValue<int>("Key"));

        Assert.Contains("Key", ex.Message, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------
    // TryGetValue
    // ------------------------------------------------------------------

    [Fact]
    public void TryGetValueSuccess()
    {
        var result = Options("Key", "123").TryGetValue<int>("Key", out var value);

        Assert.True(result);
        Assert.Equal(123, value);
    }

    [Fact]
    public void TryGetValueInvalidReturnsFalse()
    {
        var result = Options("Key", "abc").TryGetValue<int>("Key", out var value);

        Assert.False(result);
        Assert.Equal(0, value);
    }

    [Fact]
    public void TryGetValueMissingKeyReturnsFalse()
    {
        var result = Options("Key", "1").TryGetValue<int>("Other", out _);

        Assert.False(result);
    }
}
