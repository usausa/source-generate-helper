namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public sealed class TypedConstantExtensionsTest
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
        }

        public enum Sample { A, B }

        [Flags]
        public enum Flag { None = 0, A = 1, B = 2 }

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
