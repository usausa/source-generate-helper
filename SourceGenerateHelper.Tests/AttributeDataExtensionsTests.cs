namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;

public sealed class AttributeDataExtensionsTests
{
    public enum Mode
    {
        A,
        B
    }

    private const string Source =
        """
        using System;

        public enum Mode { A, B }

        public sealed class TestAttribute : Attribute
        {
            public TestAttribute() { }
            public TestAttribute(int value, string text) { }
            public TestAttribute(Mode mode) { }
            public int Number { get; set; }
            public string? Name { get; set; }
            public Mode Mode { get; set; }
            public int[]? Values { get; set; }
        }

        [Test(5, "x", Number = 1, Name = "n", Mode = Mode.B, Values = new[] { 1, 2 })] public class Full { }
        [Test] public class Empty { }
        [Test(5)] public class Incomplete { }
        [Test(Mode.Missing)] public class ErrorArgument { }
        [Test(Mode = Mode.Missing)] public class ErrorNamed { }
        [Test(Number = 1, Number = 2)] public class Duplicate { }
        [Test(Name = null)] public class NullNamed { }
        """;

    private static AttributeData GetAttribute(string typeName) =>
        TestCompilation.Create(Source).GetTypeByMetadataName(typeName)!.GetAttributes()[0];

    // ------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------

    [Fact]
    public void ConstructorArgumentIsRead()
    {
        var attribute = GetAttribute("Full");

        Assert.True(attribute.TryGetConstructorArgument<int>(0, out var value));
        Assert.Equal(5, value);
        Assert.True(attribute.TryGetConstructorArgument<string>(1, out var text));
        Assert.Equal("x", text);
    }

    [Fact]
    public void ConstructorArgumentOfOtherTypeIsNotRead()
    {
        Assert.False(GetAttribute("Full").TryGetConstructorArgument<string>(0, out _));
    }

    [Fact]
    public void MissingConstructorArgumentIsNotRead()
    {
        Assert.False(GetAttribute("Full").TryGetConstructorArgument<int>(2, out _));
        Assert.False(GetAttribute("Full").TryGetConstructorArgument<int>(-1, out _));
        Assert.False(GetAttribute("Empty").TryGetConstructorArgument<int>(0, out _));
        Assert.False(GetAttribute("Incomplete").TryGetConstructorArgument<int>(0, out _));
        Assert.False(GetAttribute("Incomplete").TryGetConstructorArgument<int>(1, out _));
    }

    [Fact]
    public void ConstructorArgumentWithErrorIsNotRead()
    {
        Assert.False(GetAttribute("ErrorArgument").TryGetConstructorArgument(0, out _));
        Assert.False(GetAttribute("ErrorArgument").TryGetConstructorArgument<Mode>(0, out _));
    }

    // ------------------------------------------------------------
    // Named
    // ------------------------------------------------------------

    [Fact]
    public void NamedArgumentIsRead()
    {
        var attribute = GetAttribute("Full");

        Assert.True(attribute.TryGetNamedArgument<int>("Number", out var number));
        Assert.Equal(1, number);
        Assert.True(attribute.TryGetNamedArgument<string>("Name", out var name));
        Assert.Equal("n", name);
    }

    [Fact]
    public void EnumArgumentIsReadAsEnum()
    {
        Assert.True(GetAttribute("Full").TryGetNamedArgument<Mode>("Mode", out var mode));
        Assert.Equal(Mode.B, mode);
        Assert.True(GetAttribute("Full").TryGetNamedArgument<int>("Mode", out var number));
        Assert.Equal(1, number);
    }

    [Fact]
    public void ArrayArgumentIsReadAsValues()
    {
        Assert.True(GetAttribute("Full").TryGetNamedArgument("Values", out var constant));
        Assert.True(constant.TryGetValues<int>(out var values));
        Assert.Equal([1, 2], values);
        Assert.False(GetAttribute("Full").TryGetNamedArgument<int[]>("Values", out _));
    }

    [Fact]
    public void MissingNamedArgumentIsNotRead()
    {
        Assert.False(GetAttribute("Full").TryGetNamedArgument<int>("Missing", out _));
        Assert.False(GetAttribute("Empty").TryGetNamedArgument<int>("Number", out _));
    }

    [Fact]
    public void NamedArgumentWithErrorIsNotRead()
    {
        Assert.False(GetAttribute("ErrorNamed").TryGetNamedArgument("Mode", out _));
        Assert.False(GetAttribute("ErrorNamed").TryGetNamedArgument<Mode>("Mode", out _));
    }

    [Fact]
    public void LastOfDuplicateNamedArgumentIsRead()
    {
        Assert.True(GetAttribute("Duplicate").TryGetNamedArgument<int>("Number", out var number));
        Assert.Equal(2, number);
    }

    [Fact]
    public void NullNamedArgumentIsGivenButHasNoValue()
    {
        Assert.True(GetAttribute("NullNamed").TryGetNamedArgument("Name", out var constant));
        Assert.True(constant.IsNull);
        Assert.False(GetAttribute("NullNamed").TryGetNamedArgument<string>("Name", out _));
    }
}
