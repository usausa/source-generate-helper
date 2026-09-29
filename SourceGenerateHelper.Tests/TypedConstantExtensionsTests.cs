namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public sealed class TypedConstantExtensionsTests
{
    private const string Source =
        """
        using System;

        public sealed class ValAttribute : Attribute
        {
            public ValAttribute(int v) { }
            public ValAttribute(long v) { }
            public ValAttribute(uint v) { }
            public ValAttribute(ulong v) { }
            public ValAttribute(float v) { }
            public ValAttribute(double v) { }
            public ValAttribute(char v) { }
            public ValAttribute(bool v) { }
            public ValAttribute(string v) { }
            public ValAttribute(Sample v) { }
            public ValAttribute(Flag v) { }
            public ValAttribute(Type v) { }
            public ValAttribute(object v) { }
            public ValAttribute(int[] v) { }
            public ValAttribute(byte v) { }
            public ValAttribute(short v) { }
            public ValAttribute(Keyword v) { }
            public ValAttribute(Replaced v) { }
            public ValAttribute(object[] v) { }
            public ValAttribute(byte[] v) { }
            public ValAttribute(Sample[] v) { }
        }

        public sealed class EnumOnlyAttribute : Attribute
        {
            public EnumOnlyAttribute(Sample v) { }
        }

        public enum Sample { A, B }

        [Flags]
        public enum Flag { None = 0, A = 1, B = 2 }

        public enum Keyword { @default, @class }

        public enum Replaced { [Obsolete] Old = 1, New = 1 }

        [Val((byte)5)] public class CByte { }
        [Val((short)-5)] public class CShort { }
        [Val(Keyword.@class)] public class CKeyword { }
        [Val(Replaced.New)] public class CReplaced { }
        [Val(new object[] { (byte)1, "x", null, typeof(int), Sample.A })] public class CObjectArray { }
        [Val(new byte[] { 1, 2 })] public class CByteArray { }
        [Val(new Sample[] { Sample.B })] public class CEnumArray { }
        [Val(new int[0])] public class CEmptyArray { }
        [EnumOnly((Sample)Missing.Value)] public class CError { }
        [Val(typeof(Missing))] public class CErrorType { }
        [Val(-0.0)] public class CNegativeZero { }

        [Val(123L)] public class CLong { }
        [Val(5u)] public class CUInt { }
        [Val(5uL)] public class CULong { }
        [Val(1.5f)] public class CFloat { }
        [Val(1.5d)] public class CDouble { }
        [Val("x")] public class CString { }
        [Val(Sample.B)] public class CEnum { }
        [Val(Double.NaN)] public class CNaN { }
        [Val(Double.PositiveInfinity)] public class CInfinity { }
        [Val(Single.NaN)] public class CFloatNaN { }
        [Val('a')] public class CChar { }
        [Val(true)] public class CBool { }
        [Val("a\"b\nc")] public class CEscape { }
        [Val((Flag)(Flag.A | Flag.B))] public class CFlags { }
        [Val(typeof(String))] public class CType { }
        [Val((object)null)] public class CNull { }
        [Val(new int[] { 1, 2 })] public class CArray { }
        [Val(-1)] public class CNegative { }
        [Val(1)] public class CInt { }
        """;

    // Symbols are compared by identity, so all symbols must come from the same compilation
    private static readonly CSharpCompilation Compilation = CSharpCompilation.Create(
        "TestAssembly",
        [CSharpSyntaxTree.ParseText(Source)],
        [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static TypedConstant GetConstant(string typeName)
    {
        var type = Compilation.GetTypeByMetadataName(typeName)!;
        return type.GetAttributes().First().ConstructorArguments[0];
    }

    private static ITypeSymbol GetSpecialType(SpecialType specialType) =>
        Compilation.GetSpecialType(specialType);

    // ------------------------------------------------------------
    // Postfix
    // ------------------------------------------------------------

    [Fact]
    public void LongHasPostfix()
    {
        Assert.Equal("123L", GetConstant("CLong").ToCSharpStringWithPostfix());
    }

    [Fact]
    public void UIntHasPostfix()
    {
        Assert.Equal("5u", GetConstant("CUInt").ToCSharpStringWithPostfix());
    }

    [Fact]
    public void ULongHasPostfix()
    {
        Assert.Equal("5uL", GetConstant("CULong").ToCSharpStringWithPostfix());
    }

    [Fact]
    public void FloatHasPostfix()
    {
        Assert.Equal("1.5f", GetConstant("CFloat").ToCSharpStringWithPostfix());
    }

    [Fact]
    public void DoubleHasPostfix()
    {
        Assert.Equal("1.5d", GetConstant("CDouble").ToCSharpStringWithPostfix());
    }

    [Fact]
    public void StringHasNoPostfix()
    {
        var constant = GetConstant("CString");

        Assert.Equal(constant.ToCSharpString(), constant.ToCSharpStringWithPostfix());
    }

    [Fact]
    public void EnumHasNoPostfix()
    {
        var constant = GetConstant("CEnum");

        Assert.Equal(constant.ToCSharpString(), constant.ToCSharpStringWithPostfix());
    }

    [Fact]
    public void PostfixForNotFiniteIsValidCSharp()
    {
        Assert.Equal("double.NaN", GetConstant("CNaN").ToCSharpStringWithPostfix());
        Assert.Equal("double.PositiveInfinity", GetConstant("CInfinity").ToCSharpStringWithPostfix());
        Assert.Equal("float.NaN", GetConstant("CFloatNaN").ToCSharpStringWithPostfix());
    }

    // ------------------------------------------------------------
    // Expression
    // ------------------------------------------------------------

    [Fact]
    public void PrimitiveExpression()
    {
        Assert.Equal("1", GetConstant("CInt").ToCSharpExpression());
        Assert.Equal("123L", GetConstant("CLong").ToCSharpExpression());
        Assert.Equal("5u", GetConstant("CUInt").ToCSharpExpression());
        Assert.Equal("5uL", GetConstant("CULong").ToCSharpExpression());
        Assert.Equal("1.5f", GetConstant("CFloat").ToCSharpExpression());
        Assert.Equal("1.5d", GetConstant("CDouble").ToCSharpExpression());
        Assert.Equal("true", GetConstant("CBool").ToCSharpExpression());
        Assert.Equal("'a'", GetConstant("CChar").ToCSharpExpression());
        Assert.Equal("\"x\"", GetConstant("CString").ToCSharpExpression());
    }

    [Fact]
    public void StringExpressionIsEscaped()
    {
        Assert.Equal("\"a\\\"b\\nc\"", GetConstant("CEscape").ToCSharpExpression());
    }

    [Fact]
    public void NotFiniteExpressionIsValidCSharp()
    {
        Assert.Equal("double.NaN", GetConstant("CNaN").ToCSharpExpression());
        Assert.Equal("double.PositiveInfinity", GetConstant("CInfinity").ToCSharpExpression());
        Assert.Equal("float.NaN", GetConstant("CFloatNaN").ToCSharpExpression());
    }

    [Fact]
    public void NullExpression()
    {
        Assert.Equal("null", GetConstant("CNull").ToCSharpExpression());
    }

    [Fact]
    public void EnumExpressionUsesMemberName()
    {
        Assert.Equal("global::Sample.B", GetConstant("CEnum").ToCSharpExpression());
    }

    [Fact]
    public void CombinedFlagsExpressionUsesCast()
    {
        Assert.Equal("(global::Flag)(3)", GetConstant("CFlags").ToCSharpExpression());
    }

    [Fact]
    public void TypeExpressionUsesTypeof()
    {
        Assert.Equal("typeof(string)", GetConstant("CType").ToCSharpExpression());
    }

    [Fact]
    public void ArrayExpressionIsNotSupported()
    {
        Assert.Null(GetConstant("CArray").ToCSharpExpression());
        Assert.Null(GetConstant("CArray").ToCSharpExpression(Compilation.CreateArrayTypeSymbol(GetSpecialType(SpecialType.System_Int32))));
    }

    [Fact]
    public void SmallIntegerExpressionIsCast()
    {
        Assert.Equal("(byte)5", GetConstant("CByte").ToCSharpExpression());
        Assert.Equal("(short)-5", GetConstant("CShort").ToCSharpExpression());
        Assert.Equal("(int)(short)-5", GetConstant("CShort").ToCSharpExpression(GetSpecialType(SpecialType.System_Int32)));
    }

    [Fact]
    public void KeywordEnumMemberIsEscaped()
    {
        Assert.Equal("global::Keyword.@class", GetConstant("CKeyword").ToCSharpExpression());
    }

    [Fact]
    public void ObsoleteEnumMemberIsPassedOver()
    {
        Assert.Equal("global::Replaced.New", GetConstant("CReplaced").ToCSharpExpression());
    }

    [Fact]
    public void ArgumentWithErrorHasNoExpression()
    {
        var constant = GetConstant("CError");

        Assert.Equal(TypedConstantKind.Error, constant.Kind);
        Assert.Null(constant.ToCSharpExpression());
        Assert.Null(constant.ToCSharpExpression(GetSpecialType(SpecialType.System_Int32)));
    }

    [Fact]
    public void TypeWithErrorHasNoExpressionOrValue()
    {
        var constant = GetConstant("CErrorType");

        Assert.Equal(TypedConstantKind.Type, constant.Kind);
        Assert.Null(constant.ToCSharpExpression());
        Assert.False(constant.TryGetValue<ITypeSymbol>(out _));
    }

    [Fact]
    public void NegativeZeroKeepsSign()
    {
        Assert.Equal("-0d", GetConstant("CNegativeZero").ToCSharpExpression());
    }

    private static readonly string[] ExpressionTypes =
    [
        "CLong", "CFloat", "CString", "CEnum", "CFlags", "CType", "CNull", "CByte", "CShort", "CKeyword", "CReplaced", "CEscape", "CNaN"
    ];

    private static readonly string[] ArrayTypes = ["CArray", "CObjectArray", "CByteArray", "CEnumArray", "CEmptyArray"];

    [Fact]
    public void ExpressionsCompile()
    {
        var expressions = ExpressionTypes.Select(static x => GetConstant(x).ToCSharpExpression())
            .Concat(ArrayTypes.Select(x => TryConvert(x, GetConstant(x).Type!, out var expression) ? expression : "missing"));
        var source = $"#nullable enable\npublic static class Values {{ public static object?[] Get() => [{String.Join(", ", expressions)}]; }}";
        var compilation = TestCompilation.AddSource(Compilation, source, "Values.cs");

        // The attributes with errors have errors of their own
        Assert.DoesNotContain(
            TestCompilation.GetProblems(compilation),
            static x => (x.Severity == DiagnosticSeverity.Error) && (x.Location.SourceTree?.FilePath == "Values.cs"));
    }

    // ------------------------------------------------------------
    // Value
    // ------------------------------------------------------------

    public enum MirrorSample
    {
        A,
        B
    }

    [Fact]
    public void ValueOfTypeIsRead()
    {
        Assert.True(GetConstant("CInt").TryGetValue<int>(out var number));
        Assert.Equal(1, number);
        Assert.True(GetConstant("CString").TryGetValue<string>(out var text));
        Assert.Equal("x", text);
        Assert.True(GetConstant("CType").TryGetValue<ITypeSymbol>(out var type));
        Assert.Equal(SpecialType.System_String, type.SpecialType);
    }

    [Fact]
    public void EnumValueIsReadAsNumberOrEnum()
    {
        Assert.True(GetConstant("CEnum").TryGetValue<int>(out var number));
        Assert.Equal(1, number);
        Assert.True(GetConstant("CEnum").TryGetValue<MirrorSample>(out var mirror));
        Assert.Equal(MirrorSample.B, mirror);
    }

    [Fact]
    public void ValueOfOtherTypeOrNullOrErrorIsNotRead()
    {
        Assert.False(GetConstant("CInt").TryGetValue<string>(out _));
        Assert.False(GetConstant("CLong").TryGetValue<int>(out _));
        Assert.False(GetConstant("CNull").TryGetValue<object>(out _));
        Assert.False(GetConstant("CArray").TryGetValue<int[]>(out _));
        Assert.False(GetConstant("CError").TryGetValue<int>(out _));
    }

    [Fact]
    public void ValuesOfArrayAreRead()
    {
        Assert.True(GetConstant("CArray").TryGetValues<int>(out var values));
        Assert.Equal([1, 2], values);
        Assert.True(GetConstant("CEmptyArray").TryGetValues<int>(out var empty));
        Assert.Empty(empty);
    }

    [Fact]
    public void NullElementIsDefault()
    {
        Assert.True(GetConstant("CObjectArray").TryGetValues<object?>(out var values));
        Assert.Equal(5, values.Length);
        Assert.Null(values[2]);
    }

    [Fact]
    public void ValuesOfOtherTypeOrNotArrayAreNotRead()
    {
        Assert.False(GetConstant("CArray").TryGetValues<string>(out _));
        Assert.False(GetConstant("CObjectArray").TryGetValues<string>(out _));
        Assert.False(GetConstant("CInt").TryGetValues<int>(out _));
        Assert.False(GetConstant("CNull").TryGetValues<int>(out _));
    }

    // ------------------------------------------------------------
    // Conversion
    // ------------------------------------------------------------

    private static bool TryConvert(string typeName, ITypeSymbol targetType, out string? expression)
    {
        var tree = Compilation.SyntaxTrees[0];
        return GetConstant(typeName).TryToCSharpExpression(targetType, Compilation.GetSemanticModel(tree), 0, out expression);
    }

    [Fact]
    public void ValueConvertingImplicitlyIsTaken()
    {
        Assert.True(TryConvert("CInt", GetSpecialType(SpecialType.System_Double), out var widened));
        Assert.Equal("1", widened);
        Assert.True(TryConvert("CInt", GetSpecialType(SpecialType.System_Byte), out _));
        Assert.True(TryConvert("CNull", GetSpecialType(SpecialType.System_String), out _));
        Assert.True(TryConvert("CEnum", Compilation.GetTypeByMetadataName("Sample")!, out var member));
        Assert.Equal("global::Sample.B", member);
        Assert.True(TryConvert("CType", Compilation.GetTypeByMetadataName("System.Type")!, out _));
        Assert.True(TryConvert("CByte", GetSpecialType(SpecialType.System_Object), out var boxed));
        Assert.Equal("(byte)5", boxed);
    }

    [Fact]
    public void ArrayIsWrittenWhenTargetTakesIt()
    {
        var intArray = Compilation.CreateArrayTypeSymbol(GetSpecialType(SpecialType.System_Int32));

        Assert.True(TryConvert("CArray", intArray, out var ints));
        Assert.Equal("new int[] { 1, 2 }", ints);
        Assert.True(TryConvert("CEmptyArray", intArray, out var empty));
        Assert.Equal("new int[0]", empty);
        Assert.True(TryConvert("CByteArray", Compilation.CreateArrayTypeSymbol(GetSpecialType(SpecialType.System_Byte)), out var bytes));
        Assert.Equal("new byte[] { 1, 2 }", bytes);
        Assert.True(TryConvert("CEnumArray", Compilation.CreateArrayTypeSymbol(Compilation.GetTypeByMetadataName("Sample")!), out var enums));
        Assert.Equal("new global::Sample[] { global::Sample.B }", enums);
        Assert.True(TryConvert("CObjectArray", Compilation.CreateArrayTypeSymbol(GetSpecialType(SpecialType.System_Object)), out var objects));
        Assert.Equal("new object?[] { (byte)1, \"x\", null, typeof(int), global::Sample.A }", objects);
        Assert.False(TryConvert("CArray", Compilation.CreateArrayTypeSymbol(GetSpecialType(SpecialType.System_String)), out _));
    }

    [Fact]
    public void ValueOfOtherTypeIsRefused()
    {
        Assert.False(TryConvert("CDouble", GetSpecialType(SpecialType.System_Int32), out _));
        Assert.False(TryConvert("CString", GetSpecialType(SpecialType.System_Int32), out _));
        Assert.False(TryConvert("CNull", GetSpecialType(SpecialType.System_Int32), out _));
        Assert.False(TryConvert("CInt", Compilation.GetTypeByMetadataName("Sample")!, out _));
        Assert.False(TryConvert("CError", GetSpecialType(SpecialType.System_Int32), out _));
    }

    // ------------------------------------------------------------
    // Target type
    // ------------------------------------------------------------

    [Fact]
    public void DifferentTargetTypeIsCast()
    {
        Assert.Equal("(double)1", GetConstant("CInt").ToCSharpExpression(GetSpecialType(SpecialType.System_Double)));
    }

    [Fact]
    public void NegativeValueIsParenthesized()
    {
        Assert.Equal("(double)(-1)", GetConstant("CNegative").ToCSharpExpression(GetSpecialType(SpecialType.System_Double)));
    }

    [Fact]
    public void SameTargetTypeIsNotCast()
    {
        Assert.Equal("1", GetConstant("CInt").ToCSharpExpression(GetSpecialType(SpecialType.System_Int32)));
    }

    [Fact]
    public void EnumIsNotCastToTargetType()
    {
        Assert.Equal("global::Sample.B", GetConstant("CEnum").ToCSharpExpression(GetSpecialType(SpecialType.System_Object)));
    }
}
