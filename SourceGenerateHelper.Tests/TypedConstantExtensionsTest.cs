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
            public ValAttribute(long v) { }
            public ValAttribute(uint v) { }
            public ValAttribute(ulong v) { }
            public ValAttribute(float v) { }
            public ValAttribute(double v) { }
            public ValAttribute(string v) { }
            public ValAttribute(Sample v) { }
        }

        public enum Sample { A, B }

        [Val(123L)] public class CLong { }
        [Val(5u)] public class CUInt { }
        [Val(5uL)] public class CULong { }
        [Val(1.5f)] public class CFloat { }
        [Val(1.5d)] public class CDouble { }
        [Val("x")] public class CString { }
        [Val(Sample.B)] public class CEnum { }
        """;

    private static TypedConstant GetConstant(string typeName)
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(Source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var type = compilation.GetTypeByMetadataName(typeName)!;
        return type.GetAttributes().First().ConstructorArguments[0];
    }

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
}
