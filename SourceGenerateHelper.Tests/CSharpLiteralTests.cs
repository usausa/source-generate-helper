namespace SourceGenerateHelper.Tests;

using System.Globalization;

using Microsoft.CodeAnalysis;

public sealed class CSharpLiteralTests
{
    // ------------------------------------------------------------
    // Text
    // ------------------------------------------------------------

    [Fact]
    public void StringIsEscaped()
    {
        Assert.Equal("\"a\\\"b\\\\c\\r\\n\\td\"", CSharpLiteral.Format("a\"b\\c\r\n\td"));
    }

    [Fact]
    public void CharIsEscaped()
    {
        Assert.Equal("'\\''", CSharpLiteral.Format('\''));
        Assert.Equal("'\\\\'", CSharpLiteral.Format('\\'));
        Assert.Equal("'\\n'", CSharpLiteral.Format('\n'));
    }

    // ------------------------------------------------------------
    // Value
    // ------------------------------------------------------------

    [Fact]
    public void NumberHasSuffixOfItsType()
    {
        Assert.Equal("1", CSharpLiteral.Format(1));
        Assert.Equal("-1", CSharpLiteral.Format(-1));
        Assert.Equal("1L", CSharpLiteral.Format(1L));
        Assert.Equal("1u", CSharpLiteral.Format(1u));
        Assert.Equal("1uL", CSharpLiteral.Format(1uL));
        Assert.Equal("1.50m", CSharpLiteral.Format(1.50m));
        Assert.Equal("1.5f", CSharpLiteral.Format(1.5f));
        Assert.Equal("0.1d", CSharpLiteral.Format(0.1d));
    }

    [Fact]
    public void SmallIntegerIsCast()
    {
        Assert.Equal("(byte)1", CSharpLiteral.Format((byte)1));
        Assert.Equal("(sbyte)-1", CSharpLiteral.Format((sbyte)-1));
        Assert.Equal("(short)-1", CSharpLiteral.Format((short)-1));
        Assert.Equal("(ushort)1", CSharpLiteral.Format((ushort)1));
    }

    [Fact]
    public void NotFiniteIsConstant()
    {
        Assert.Equal("double.NaN", CSharpLiteral.Format(Double.NaN));
        Assert.Equal("double.PositiveInfinity", CSharpLiteral.Format(Double.PositiveInfinity));
        Assert.Equal("double.NegativeInfinity", CSharpLiteral.Format(Double.NegativeInfinity));
        Assert.Equal("float.NaN", CSharpLiteral.Format(Single.NaN));
        Assert.Equal("float.NegativeInfinity", CSharpLiteral.Format(Single.NegativeInfinity));
    }

    [Fact]
    public void NegativeZeroKeepsSign()
    {
        Assert.Equal("-0d", CSharpLiteral.Format(-0.0d));
        Assert.Equal("-0f", CSharpLiteral.Format(-0.0f));
    }

    [Fact]
    public void OtherValues()
    {
        Assert.Equal("null", CSharpLiteral.Format((object?)null));
        Assert.Equal("true", CSharpLiteral.Format(true));
        Assert.Equal("\"x\"", CSharpLiteral.Format((object)"x"));
        Assert.Equal("'x'", CSharpLiteral.Format((object)'x'));
        Assert.Null(CSharpLiteral.Format(DateTime.MinValue));
    }

    [Fact]
    public void FormatDoesNotDependOnCulture()
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1.5d", CSharpLiteral.Format(1.5d));
            Assert.Equal("1.5m", CSharpLiteral.Format(1.5m));
            Assert.Equal("-1", CSharpLiteral.Format(-1));
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void LiteralsCompileToTheirValues()
    {
        object[] values =
        [
            0, -1, Int32.MinValue, Int32.MaxValue, Int64.MinValue, Int64.MaxValue, UInt32.MaxValue, UInt64.MaxValue,
            (byte)255, (sbyte)-128, Int16.MinValue, UInt16.MaxValue,
            0.1d, 1e20d, -1.5e-300d, Double.Epsilon, Double.MaxValue, Double.MinValue, Double.NaN, Double.PositiveInfinity, -0.0d,
            0.1f, 3.4e38f, Single.Epsilon, -0.0f, Single.NaN,
            1.50m, Decimal.MinValue, Decimal.MaxValue, -0.0001m,
            'a', '\0', (char)0xFFFF, '\'',
            "a\"b\\c\r\n\t\0" + (char)0x2028, string.Empty,
            true, false
        ];

        var source =
            $$"""
            public static class Values
            {
                public static object[] Get() => [{{String.Join(", ", values.Select(static x => CSharpLiteral.Format(x)))}}];
            }
            """;

        var assembly = TestCompilation.Load(TestCompilation.Create(source));
        var loaded = (object[])assembly.GetType("Values")!.GetMethod("Get")!.Invoke(null, null)!;

        Assert.Equal(values.Length, loaded.Length);
        for (var i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i].GetType(), loaded[i].GetType());
            Assert.Equal(values[i], loaded[i]);
            if (values[i] is double d)
            {
                Assert.Equal(BitConverter.DoubleToInt64Bits(d), BitConverter.DoubleToInt64Bits((double)loaded[i]));
            }
        }
    }

    // ------------------------------------------------------------
    // Enum
    // ------------------------------------------------------------

    private const string EnumSource =
        """
        using System;

        public enum Sample { A, B }

        public enum Keyword { @default, @class }

        [Flags]
        public enum Flag { None = 0, X = 1, Y = 2 }

        public enum Negative { Minus = -1 }

        public enum Replaced { [Obsolete] Old = 1, New = 1 }

        public enum OnlyObsolete { [Obsolete] Old = 1 }

        public enum Wide : long { Big = 1L << 40 }
        """;

    private static ITypeSymbol GetType(string name) =>
        TestCompilation.Create(EnumSource).GetTypeByMetadataName(name)!;

    [Fact]
    public void EnumValueIsMember()
    {
        Assert.Equal("global::Sample.B", CSharpLiteral.FormatEnum(GetType("Sample"), 1));
    }

    [Fact]
    public void KeywordMemberIsEscaped()
    {
        Assert.Equal("global::Keyword.@class", CSharpLiteral.FormatEnum(GetType("Keyword"), 1));
    }

    [Fact]
    public void ValueWithoutMemberIsCast()
    {
        Assert.Equal("(global::Flag)(3)", CSharpLiteral.FormatEnum(GetType("Flag"), 3));
        Assert.Equal("(global::Negative)(-2)", CSharpLiteral.FormatEnum(GetType("Negative"), -2));
        Assert.Equal("(global::Wide)(3L)", CSharpLiteral.FormatEnum(GetType("Wide"), 3L));
    }

    [Fact]
    public void ObsoleteMemberIsPassedOver()
    {
        Assert.Equal("global::Replaced.New", CSharpLiteral.FormatEnum(GetType("Replaced"), 1));
        Assert.Equal("(global::OnlyObsolete)(1)", CSharpLiteral.FormatEnum(GetType("OnlyObsolete"), 1));
    }

    [Fact]
    public void EnumOfOtherValueIsNull()
    {
        Assert.Null(CSharpLiteral.FormatEnum(GetType("Sample"), "A"));
        Assert.Null(CSharpLiteral.FormatEnum(GetType("Sample"), null));
    }

    [Fact]
    public void EnumValuesCompile()
    {
        var compilation = TestCompilation.Create(EnumSource);
        var expressions = new[]
        {
            CSharpLiteral.FormatEnum(compilation.GetTypeByMetadataName("Keyword")!, 1),
            CSharpLiteral.FormatEnum(compilation.GetTypeByMetadataName("Flag")!, 3),
            CSharpLiteral.FormatEnum(compilation.GetTypeByMetadataName("Negative")!, -2),
            CSharpLiteral.FormatEnum(compilation.GetTypeByMetadataName("Replaced")!, 1),
            CSharpLiteral.FormatEnum(compilation.GetTypeByMetadataName("OnlyObsolete")!, 1),
            CSharpLiteral.FormatEnum(compilation.GetTypeByMetadataName("Wide")!, 3L)
        };
        var source = $"public static class Values {{ public static object[] Get() => [{String.Join(", ", expressions)}]; }}";

        Assert.Empty(TestCompilation.GetProblems(TestCompilation.AddSource(compilation, source)));
    }
}
