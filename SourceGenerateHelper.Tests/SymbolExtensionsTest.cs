namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public sealed class SymbolExtensionsTest
{
    // ------------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------------

    private const string TestSource =
        """
        namespace MyNs
        {
            public class BaseClass
            {
                public int PublicProp { get; set; }
                public static int StaticProp { get; set; }
                internal string InternalProp { get; set; } = string.Empty;
            }

            public class DerivedClass : BaseClass
            {
                public string DerivedProp { get; set; } = string.Empty;
            }

            public class UnrelatedClass { }

            public class MyClass<T> { }

            public interface IMyInterface { }

            public class ImplementsMyInterface : IMyInterface { }

            namespace Nested
            {
                public class NestedType { }
            }
        }
        """;

    private const string NullableSource =
        """
        #nullable enable
        namespace MyNs
        {
            public class NullableHolder
            {
                public string? NullableStringProp { get; set; }
                public string NonNullableStringProp { get; set; } = string.Empty;
            }
        }
        """;

    private const string EnumSource =
        """
        namespace MyNs
        {
            public enum MyEnum { A, B }
            public enum MyByteEnum : byte { X, Y }
        }
        """;

    private static CSharpCompilation CreateCompilation(params string[] sources)
    {
        var coreLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var trees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();
        return CSharpCompilation.Create(
            "TestAssembly",
            trees,
            [coreLib],
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    // ------------------------------------------------------------------
    // GetTypeMembersRecursive
    // ------------------------------------------------------------------

    [Fact]
    public void GetTypeMembersRecursiveIncludesNestedNamespaceTypes()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);

        // Act
        var types = compilation.Assembly.GlobalNamespace
            .GetTypeMembersRecursive()
            .Select(static t => t.Name)
            .ToList();

        // Assert
        Assert.Contains("BaseClass", types);
        Assert.Contains("NestedType", types);
    }

    [Fact]
    public void GetTypeMembersRecursiveWithPredicateFilters()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);

        // Act
        var types = compilation.Assembly.GlobalNamespace
            .GetTypeMembersRecursive(static t => t.Name.StartsWith("My", StringComparison.Ordinal))
            .Select(static t => t.Name)
            .ToList();

        // Assert
        Assert.Contains("MyClass", types);
        Assert.DoesNotContain("BaseClass", types);
    }

    // ------------------------------------------------------------------
    // GetClassName
    // ------------------------------------------------------------------

    [Fact]
    public void GetClassNameNonGeneric()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var type = compilation.GetTypeByMetadataName("MyNs.BaseClass")!;

        // Act & Assert
        Assert.Equal("BaseClass", type.GetClassName());
    }

    [Fact]
    public void GetClassNameGenericDefinition()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var type = compilation.GetTypeByMetadataName("MyNs.MyClass`1")!;

        // Act & Assert
        Assert.Equal("MyClass<T>", type.GetClassName());
    }

    // ------------------------------------------------------------------
    // IsGenericType
    // ------------------------------------------------------------------

    [Fact]
    public void IsGenericTypeTrueForGenericClass()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var type = compilation.GetTypeByMetadataName("MyNs.MyClass`1")!;

        // Act & Assert
        Assert.True(type.IsGenericType());
    }

    [Fact]
    public void IsGenericTypeTrueForTypeParameter()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var type = compilation.GetTypeByMetadataName("MyNs.MyClass`1")!;
        var typeParam = type.TypeParameters[0];

        // Act & Assert
        Assert.True(typeParam.IsGenericType());
    }

    [Fact]
    public void IsGenericTypeFalseForNonGeneric()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var type = compilation.GetTypeByMetadataName("MyNs.BaseClass")!;

        // Act & Assert
        Assert.False(type.IsGenericType());
    }

    // ------------------------------------------------------------------
    // IsNullableType / GetUnderlyingType
    // ------------------------------------------------------------------

    [Fact]
    public void IsNullableTypeTrueForNullableInt()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var nullableInt = compilation.GetTypeByMetadataName("System.Nullable`1")!.Construct(intSymbol);

        // Act & Assert
        Assert.True(nullableInt.IsNullableType());
    }

    [Fact]
    public void GetUnderlyingTypeForNullableInt()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var nullableInt = compilation.GetTypeByMetadataName("System.Nullable`1")!.Construct(intSymbol);

        // Act
        var underlying = nullableInt.GetUnderlyingType();

        // Assert
        Assert.Equal(SpecialType.System_Int32, underlying.SpecialType);
    }

    [Fact]
    public void IsNullableTypeTrueForAnnotatedString()
    {
        // Arrange
        var compilation = CreateCompilation(NullableSource);
        var holderType = compilation.GetTypeByMetadataName("MyNs.NullableHolder")!;
        var prop = holderType.GetMembers("NullableStringProp").OfType<IPropertySymbol>().Single();

        // Act & Assert
        Assert.True(prop.Type.IsNullableType());
    }

    [Fact]
    public void GetUnderlyingTypeForAnnotatedString()
    {
        // Arrange
        var compilation = CreateCompilation(NullableSource);
        var holderType = compilation.GetTypeByMetadataName("MyNs.NullableHolder")!;
        var prop = holderType.GetMembers("NullableStringProp").OfType<IPropertySymbol>().Single();

        // Act
        var underlying = prop.Type.GetUnderlyingType();

        // Assert
        Assert.Equal(NullableAnnotation.NotAnnotated, underlying.NullableAnnotation);
        Assert.Equal(SpecialType.System_String, underlying.SpecialType);
    }

    [Fact]
    public void IsNullableTypeFalseForNonNullable()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var type = compilation.GetTypeByMetadataName("System.Int32")!;

        // Act & Assert
        Assert.False(type.IsNullableType());
    }

    [Fact]
    public void GetUnderlyingTypeNonNullableReturnsSelf()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;

        // Act
        var underlying = intSymbol.GetUnderlyingType();

        // Assert
        Assert.Equal(intSymbol, underlying, SymbolEqualityComparer.Default);
    }

    // ------------------------------------------------------------------
    // InheritsFrom
    // ------------------------------------------------------------------

    [Fact]
    public void InheritsFromTrueForDirectBase()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var derived = compilation.GetTypeByMetadataName("MyNs.DerivedClass")!;

        // Act & Assert
        Assert.True(derived.InheritsFrom("MyNs.BaseClass"));
    }

    [Fact]
    public void InheritsFromFalseForUnrelated()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var derived = compilation.GetTypeByMetadataName("MyNs.DerivedClass")!;

        // Act & Assert
        Assert.False(derived.InheritsFrom("MyNs.UnrelatedClass"));
    }

    // ------------------------------------------------------------------
    // IsAssignableTo
    // ------------------------------------------------------------------

    [Fact]
    public void IsAssignableToSameType()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var type = compilation.GetTypeByMetadataName("MyNs.BaseClass")!;

        // Act & Assert
        Assert.True(type.IsAssignableTo(type));
    }

    [Fact]
    public void IsAssignableToDerivedToBase()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var derived = compilation.GetTypeByMetadataName("MyNs.DerivedClass")!;
        var @base = compilation.GetTypeByMetadataName("MyNs.BaseClass")!;

        // Act & Assert
        Assert.True(derived.IsAssignableTo(@base));
    }

    [Fact]
    public void IsAssignableToImplementsInterface()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var impl = compilation.GetTypeByMetadataName("MyNs.ImplementsMyInterface")!;
        var iface = compilation.GetTypeByMetadataName("MyNs.IMyInterface")!;

        // Act & Assert
        Assert.True(impl.IsAssignableTo(iface));
    }

    [Fact]
    public void IsAssignableToAnnotatedTarget()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var derived = compilation.GetTypeByMetadataName("MyNs.DerivedClass")!;
        var @base = compilation.GetTypeByMetadataName("MyNs.BaseClass")!;
        var annotatedBase = @base.WithNullableAnnotation(NullableAnnotation.Annotated);

        // Act & Assert
        Assert.True(derived.IsAssignableTo(annotatedBase));
    }

    [Fact]
    public void IsAssignableToFalseForUnrelated()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var a = compilation.GetTypeByMetadataName("MyNs.BaseClass")!;
        var b = compilation.GetTypeByMetadataName("MyNs.UnrelatedClass")!;

        // Act & Assert
        Assert.False(a.IsAssignableTo(b));
    }

    // ------------------------------------------------------------------
    // IsImplementGenericInterface
    // ------------------------------------------------------------------

    [Fact]
    public void IsImplementGenericInterfaceTrueForListInt()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var listDef = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!;
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var listInt = listDef.Construct(intSymbol);
        var iEnumerableDef = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")!;

        // Act & Assert
        Assert.True(listInt.IsImplementGenericInterface(iEnumerableDef));
    }

    [Fact]
    public void IsImplementGenericInterfaceFalseForInt()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var iEnumerableDef = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")!;

        // Act & Assert
        Assert.False(intSymbol.IsImplementGenericInterface(iEnumerableDef));
    }

    // ------------------------------------------------------------------
    // IsImplementsInterfaceByName
    // ------------------------------------------------------------------

    [Fact]
    public void IsImplementsInterfaceByNameTrueForKnownInterface()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var impl = compilation.GetTypeByMetadataName("MyNs.ImplementsMyInterface")!;

        // Act & Assert
        Assert.True(impl.IsImplementsInterfaceByName("MyNs.IMyInterface"));
    }

    [Fact]
    public void IsImplementsInterfaceByNameFalseForUnknownName()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var impl = compilation.GetTypeByMetadataName("MyNs.ImplementsMyInterface")!;

        // Act & Assert
        Assert.False(impl.IsImplementsInterfaceByName("MyNs.IDoesNotExist"));
    }

    // ------------------------------------------------------------------
    // GetCollectionElementType
    // ------------------------------------------------------------------

    [Fact]
    public void GetCollectionElementTypeForArray()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var arrayType = compilation.CreateArrayTypeSymbol(intSymbol);

        // Act
        var elementType = arrayType.GetCollectionElementType();

        // Assert
        Assert.Equal(SpecialType.System_Int32, elementType!.SpecialType);
    }

    [Fact]
    public void GetCollectionElementTypeForListInt()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var listDef = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!;
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var listInt = listDef.Construct(intSymbol);

        // Act
        var elementType = listInt.GetCollectionElementType();

        // Assert
        Assert.Equal(SpecialType.System_Int32, elementType!.SpecialType);
    }

    [Fact]
    public void GetCollectionElementTypeNullForNonCollection()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;

        // Act & Assert
        Assert.Null(intSymbol.GetCollectionElementType());
    }

    // ------------------------------------------------------------------
    // GetAllPublicProperties
    // ------------------------------------------------------------------

    [Fact]
    public void GetAllPublicPropertiesReturnsPublicInstanceOnly()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var derived = compilation.GetTypeByMetadataName("MyNs.DerivedClass")!;

        // Act
        var props = derived.GetAllPublicProperties();
        var names = props.Select(static p => p.Name).ToList();

        // Assert
        Assert.Contains("PublicProp", names);
        Assert.Contains("DerivedProp", names);
        Assert.DoesNotContain("StaticProp", names);
        Assert.DoesNotContain("InternalProp", names);
        Assert.Equal(2, names.Count);
    }

    // ------------------------------------------------------------------
    // IsNumericType
    // ------------------------------------------------------------------

    [Fact]
    public void IsNumericTypeTrueForInt()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;

        // Act & Assert
        Assert.True(intSymbol.IsNumericType());
    }

    [Fact]
    public void IsNumericTypeTrueForByte()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var byteSymbol = compilation.GetTypeByMetadataName("System.Byte")!;

        // Act & Assert
        Assert.True(byteSymbol.IsNumericType());
    }

    [Fact]
    public void IsNumericTypeTrueForUlong()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var ulongSymbol = compilation.GetTypeByMetadataName("System.UInt64")!;

        // Act & Assert
        Assert.True(ulongSymbol.IsNumericType());
    }

    [Fact]
    public void IsNumericTypeFalseForDouble()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var doubleSymbol = compilation.GetTypeByMetadataName("System.Double")!;

        // Act & Assert
        Assert.False(doubleSymbol.IsNumericType());
    }

    [Fact]
    public void IsNumericTypeFalseForString()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var stringSymbol = compilation.GetTypeByMetadataName("System.String")!;

        // Act & Assert
        Assert.False(stringSymbol.IsNumericType());
    }

    // ------------------------------------------------------------------
    // GetEnumUnderlyingType
    // ------------------------------------------------------------------

    [Fact]
    public void GetEnumUnderlyingTypeForDefaultEnum()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource, EnumSource);
        var enumType = compilation.GetTypeByMetadataName("MyNs.MyEnum")!;

        // Act
        var underlying = enumType.GetEnumUnderlyingType();

        // Assert
        Assert.Equal(SpecialType.System_Int32, underlying!.SpecialType);
    }

    [Fact]
    public void GetEnumUnderlyingTypeForByteEnum()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource, EnumSource);
        var enumType = compilation.GetTypeByMetadataName("MyNs.MyByteEnum")!;

        // Act
        var underlying = enumType.GetEnumUnderlyingType();

        // Assert
        Assert.Equal(SpecialType.System_Byte, underlying!.SpecialType);
    }

    [Fact]
    public void GetEnumUnderlyingTypeForNullableEnum()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource, EnumSource);
        var enumType = compilation.GetTypeByMetadataName("MyNs.MyEnum")!;
        var nullableDef = compilation.GetTypeByMetadataName("System.Nullable`1")!;
        var nullableEnum = nullableDef.Construct(enumType);

        // Act
        var underlying = nullableEnum.GetEnumUnderlyingType();

        // Assert
        Assert.Equal(SpecialType.System_Int32, underlying!.SpecialType);
    }

    [Fact]
    public void GetEnumUnderlyingTypeNullForInt()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;

        // Act & Assert
        Assert.Null(intSymbol.GetEnumUnderlyingType());
    }
}
