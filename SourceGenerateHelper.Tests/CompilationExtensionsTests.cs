namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;

public sealed class CompilationExtensionsTests
{
    private const string Source =
        """
        using System;
        using System.Collections.Generic;

        public class Types
        {
            public (int A, string B) Named { get; set; } = default;
            public (int, string) Unnamed { get; set; } = default;
            public dynamic Dynamic { get; set; } = null!;
            public object Object { get; set; } = null!;
            public nint Native { get; set; }
            public IntPtr Pointer { get; set; }
            public string? Nullable { get; set; }
            public string NotNullable { get; set; } = "";
            public List<(int A, int B)> NamedList { get; set; } = null!;
            public List<(int, int)> UnnamedList { get; set; } = null!;
            public dynamic[] DynamicArray { get; set; } = null!;
            public object[] ObjectArray { get; set; } = null!;
            public int Int { get; set; }
            public long Long { get; set; }
            public List<int> IntList { get; set; } = null!;
            public List<long> LongList { get; set; } = null!;
        }
        """;

    private static bool HasIdentityConversion(string name, string other)
    {
        var compilation = TestCompilation.Create(Source);
        var type = compilation.GetTypeByMetadataName("Types")!;
        ITypeSymbol TypeOf(string property) => type.GetMembers(property).OfType<IPropertySymbol>().Single().Type;

        return compilation.HasIdentityConversion(TypeOf(name), TypeOf(other));
    }

    [Theory]
    [InlineData("Named", "Unnamed")]
    [InlineData("Dynamic", "Object")]
    [InlineData("Native", "Pointer")]
    [InlineData("Nullable", "NotNullable")]
    [InlineData("NamedList", "UnnamedList")]
    [InlineData("DynamicArray", "ObjectArray")]
    [InlineData("Int", "Int")]
    public void SameTypeConvertsByIdentity(string name, string other)
    {
        Assert.True(HasIdentityConversion(name, other));
        Assert.True(HasIdentityConversion(other, name));
    }

    [Theory]
    [InlineData("Int", "Long")]
    [InlineData("IntList", "LongList")]
    [InlineData("Object", "NotNullable")]
    [InlineData("Named", "NamedList")]
    public void OtherTypeDoesNotConvertByIdentity(string name, string other)
    {
        Assert.False(HasIdentityConversion(name, other));
    }
}
