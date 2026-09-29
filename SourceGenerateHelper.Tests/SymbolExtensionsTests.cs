namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public sealed class SymbolExtensionsTests
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

    private const string DeclarationSource =
        """
        namespace MyNs
        {
            public class ClassType { }
            public struct StructType { }
            public record RecordType;
            public record class RecordClassType;
            public record struct RecordStructType;
            public interface IInterfaceType { }

            public class Outer
            {
                public struct Middle
                {
                    public class Inner { }
                }
            }
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
    // GetDeclarationKeyword
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("MyNs.ClassType", "class")]
    [InlineData("MyNs.StructType", "struct")]
    [InlineData("MyNs.RecordType", "record")]
    [InlineData("MyNs.RecordClassType", "record")]
    [InlineData("MyNs.RecordStructType", "record struct")]
    [InlineData("MyNs.IInterfaceType", "interface")]
    public void GetDeclarationKeywordMatchesDeclaration(string metadataName, string keyword)
    {
        // Arrange
        var compilation = CreateCompilation(DeclarationSource);
        var type = compilation.GetTypeByMetadataName(metadataName)!;

        // Act & Assert
        Assert.Equal(keyword, type.GetDeclarationKeyword());
    }

    // ------------------------------------------------------------------
    // GetContainingTypes
    // ------------------------------------------------------------------

    [Fact]
    public void GetContainingTypesOrdersFromOutermost()
    {
        // Arrange
        var compilation = CreateCompilation(DeclarationSource);
        var type = compilation.GetTypeByMetadataName("MyNs.Outer+Middle+Inner")!;

        // Act
        var types = type.GetContainingTypes();

        // Assert
        Assert.Equal(2, types.Count);
        Assert.Equal("Outer", types[0].Name);
        Assert.Equal("Middle", types[1].Name);
    }

    [Fact]
    public void GetContainingTypesEmptyForTopLevelType()
    {
        // Arrange
        var compilation = CreateCompilation(DeclarationSource);
        var type = compilation.GetTypeByMetadataName("MyNs.ClassType")!;

        // Act & Assert
        Assert.Empty(type.GetContainingTypes());
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

    [Fact]
    public void GetCollectionElementTypeForEnumerableItself()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var enumerableInt = compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1")!.Construct(intSymbol);

        // Act
        var elementType = enumerableInt.GetCollectionElementType();

        // Assert
        Assert.Equal(SpecialType.System_Int32, elementType!.SpecialType);
    }

    [Fact]
    public void GetCollectionElementTypeNullForNullable()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var nullableInt = compilation.GetTypeByMetadataName("System.Nullable`1")!.Construct(intSymbol);

        // Act & Assert
        Assert.Null(nullableInt.GetCollectionElementType());
    }

    [Fact]
    public void GetCollectionElementTypeNullForGenericNonCollection()
    {
        // Arrange
        var compilation = CreateCompilation(TestSource);
        var intSymbol = compilation.GetTypeByMetadataName("System.Int32")!;
        var myClassInt = compilation.GetTypeByMetadataName("MyNs.MyClass`1")!.Construct(intSymbol);

        // Act & Assert
        Assert.Null(myClassInt.GetCollectionElementType());
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

    // ------------------------------------------------------------------
    // Names
    // ------------------------------------------------------------------

    private const string NameSource =
        """
        using System;
        using System.Collections.Generic;

        namespace MyNs
        {
            public class Outer
            {
                public class Inner { }
                public class Generic<T> { }
            }

            public class Generic<T, U> { }
            public class @class { }
            public class Keyworded<@event> { }
            public record Rec;
            public record struct RecStruct;
            public readonly partial struct ReadOnly { }
            public interface IItem { }
            public class BaseWithGeneric<T> { }
            public class DerivedFromGeneric : BaseWithGeneric<int> { }
            public class DerivedFromInner : Outer.Inner { }
            public class Item : IItem, IComparable<Item> { public int CompareTo(Item? other) => 0; }
        }

        namespace OtherNs
        {
            public interface IItem { }
            public class OtherItem : IItem { }
        }
        """;

    [Theory]
    [InlineData("MyNs.Outer", "MyNs.Outer", true)]
    [InlineData("MyNs.Outer+Inner", "MyNs.Outer+Inner", true)]
    [InlineData("MyNs.Outer+Inner", "MyNs.Outer.Inner", false)]
    [InlineData("MyNs.Outer+Generic`1", "MyNs.Outer+Generic`1", true)]
    [InlineData("MyNs.Generic`2", "MyNs.Generic`2", true)]
    [InlineData("MyNs.Generic`2", "MyNs.Generic", false)]
    [InlineData("MyNs.Outer", "Outer", false)]
    [InlineData("MyNs.Outer", "OtherNs.Outer", false)]
    [InlineData("MyNs.Outer", "XMyNs.Outer", false)]
    [InlineData("MyNs.Outer", "Ns.Outer", false)]
    [InlineData("System.Collections.Generic.List`1", "System.Collections.Generic.List`1", true)]
    [InlineData("System.Collections.Generic.List`1", "Collections.Generic.List`1", false)]
    public void HasFullyQualifiedMetadataNameComparesNames(string metadataName, string name, bool expected)
    {
        var compilation = TestCompilation.Create(NameSource);

        Assert.Equal(expected, compilation.GetTypeByMetadataName(metadataName).HasFullyQualifiedMetadataName(name));
    }

    [Fact]
    public void HasFullyQualifiedMetadataNameIgnoresTypeArgumentsAndAnnotations()
    {
        var compilation = TestCompilation.Create(NameSource);
        var list = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!
            .Construct(compilation.GetSpecialType(SpecialType.System_String))
            .WithNullableAnnotation(NullableAnnotation.Annotated);

        Assert.True(list.HasFullyQualifiedMetadataName("System.Collections.Generic.List`1"));
        Assert.False(((ITypeSymbol?)null).HasFullyQualifiedMetadataName("System.Object"));
    }

    [Fact]
    public void GetClassNameEscapesKeyword()
    {
        var compilation = TestCompilation.Create(NameSource);

        Assert.Equal("@class", compilation.GetTypeByMetadataName("MyNs.class")!.GetClassName());
        Assert.Equal("Keyworded<@event>", compilation.GetTypeByMetadataName("MyNs.Keyworded`1")!.GetClassName());
    }

    [Theory]
    [InlineData("MyNs.Outer", "partial class Outer")]
    [InlineData("MyNs.Generic`2", "partial class Generic<T, U>")]
    [InlineData("MyNs.Rec", "partial record Rec")]
    [InlineData("MyNs.RecStruct", "partial record struct RecStruct")]
    [InlineData("MyNs.ReadOnly", "partial struct ReadOnly")]
    [InlineData("MyNs.IItem", "partial interface IItem")]
    public void GetPartialDeclarationWritesKindAndName(string metadataName, string expected)
    {
        var compilation = TestCompilation.Create(NameSource);

        Assert.Equal(expected, compilation.GetTypeByMetadataName(metadataName)!.GetPartialDeclaration());
    }

    [Fact]
    public void InheritsFromNestedTypeByName()
    {
        var compilation = TestCompilation.Create(NameSource);

        Assert.True(compilation.GetTypeByMetadataName("MyNs.DerivedFromInner")!.InheritsFrom("MyNs.Outer.Inner"));
    }

    [Fact]
    public void InheritsFromGenericTypeByDisplayString()
    {
        var derived = TestCompilation.Create(NameSource).GetTypeByMetadataName("MyNs.DerivedFromGeneric")!;

        Assert.True(derived.InheritsFrom("MyNs.BaseWithGeneric<int>"));
        Assert.False(derived.InheritsFrom("MyNs.BaseWithGeneric"));
        Assert.True(derived.InheritsFrom("object"));
    }

    [Fact]
    public void InterfaceOfOtherNamespaceIsNotTaken()
    {
        var compilation = TestCompilation.Create(NameSource);

        Assert.False(compilation.GetTypeByMetadataName("OtherNs.OtherItem")!.IsImplementsInterfaceByName("MyNs.IItem"));
        Assert.True(compilation.GetTypeByMetadataName("MyNs.Item")!.IsImplementsInterfaceByName("MyNs.IItem"));
        Assert.True(compilation.GetTypeByMetadataName("OtherNs.OtherItem")!.IsImplementsInterfaceByName("IItem"));
    }

    [Fact]
    public void GenericInterfaceIsTakenByEachForm()
    {
        var item = TestCompilation.Create(NameSource).GetTypeByMetadataName("MyNs.Item")!;

        Assert.True(item.IsImplementsInterfaceByName("System.IComparable`1"));
        Assert.True(item.IsImplementsInterfaceByName("System.IComparable<T>"));
        Assert.True(item.IsImplementsInterfaceByName("IComparable`1"));
        Assert.False(item.IsImplementsInterfaceByName("System.Collections.IComparable`1"));
    }

    // ------------------------------------------------------------------
    // CanBeNull / ToTypeOfName
    // ------------------------------------------------------------------

    private const string ConstraintSource =
        """
        using System.Collections.Generic;

        public class Constraints<TAny, TStruct, TClass, TUnmanaged, TNotNull>
            where TStruct : struct
            where TClass : class
            where TUnmanaged : unmanaged
            where TNotNull : notnull
        {
            public (int, int) Tuple { get; set; }
            public KeyValuePair<int, int> Pair { get; set; }
        }
        """;

    [Fact]
    public void CanBeNullByType()
    {
        var compilation = TestCompilation.Create(ConstraintSource);
        var type = compilation.GetTypeByMetadataName("Constraints`5")!;
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        Assert.True(compilation.GetSpecialType(SpecialType.System_String).CanBeNull());
        Assert.False(intType.CanBeNull());
        Assert.True(compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(intType).CanBeNull());
        Assert.False(type.GetMembers("Tuple").OfType<IPropertySymbol>().Single().Type.CanBeNull());
        Assert.False(type.GetMembers("Pair").OfType<IPropertySymbol>().Single().Type.CanBeNull());
        Assert.Equal([true, false, true, false, true], type.TypeParameters.Select(static x => x.CanBeNull()));
    }

    [Fact]
    public void ToTypeOfNameCompiles()
    {
        var compilation = TestCompilation.Create("public static class Holder { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var list = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1")!;
        var types = new[]
        {
            compilation.DynamicType,
            stringType.WithNullableAnnotation(NullableAnnotation.Annotated),
            list.Construct(stringType.WithNullableAnnotation(NullableAnnotation.Annotated)),
            list.Construct(compilation.DynamicType),
            compilation.GetSpecialType(SpecialType.System_Nullable_T).Construct(compilation.GetSpecialType(SpecialType.System_Int32))
        };

        Assert.Equal("object", types[0].ToTypeOfName());
        Assert.Equal("string", types[1].ToTypeOfName());

        var source = $"public static class TypeOf {{ public static System.Type[] Get() => [{String.Join(", ", types.Select(static x => $"typeof({x.ToTypeOfName()})"))}]; }}";
        Assert.Empty(TestCompilation.GetProblems(TestCompilation.Create(source)));
    }

    // ------------------------------------------------------------------
    // Attribute / Obsolete
    // ------------------------------------------------------------------

    private const string MarkedSource =
        """
        using System;

        public class Marked
        {
            [Obsolete] public int Warn { get; set; }
            [Obsolete("x", true)] public int Error { get; set; }
            [Obsolete("x", false)] public int NotError { get; set; }
            [CLSCompliant(false)] public int Plain { get; set; }
        }
        """;

    private static IPropertySymbol GetMarked(string name) =>
        TestCompilation.Create(MarkedSource).GetTypeByMetadataName("Marked")!.GetMembers(name).OfType<IPropertySymbol>().Single();

    [Fact]
    public void IsObsoleteTellsWarningFromError()
    {
        Assert.True(GetMarked("Warn").IsObsolete(out var warnIsError));
        Assert.False(warnIsError);
        Assert.True(GetMarked("Error").IsObsolete(out var errorIsError));
        Assert.True(errorIsError);
        Assert.True(GetMarked("NotError").IsObsolete(out var notErrorIsError));
        Assert.False(notErrorIsError);
        Assert.False(GetMarked("Plain").IsObsolete());
    }

    [Fact]
    public void FindAttributeByMetadataName()
    {
        Assert.NotNull(GetMarked("Warn").FindAttribute("System.ObsoleteAttribute"));
        Assert.True(GetMarked("Plain").HasAttribute("System.CLSCompliantAttribute"));
        Assert.False(GetMarked("Plain").HasAttribute("System.ObsoleteAttribute"));
        Assert.Null(GetMarked("Plain").FindAttribute("Other.CLSCompliantAttribute"));
    }

    // ------------------------------------------------------------------
    // GetDefaultValueExpression
    // ------------------------------------------------------------------

    private const string DefaultSource =
        """
        #nullable enable
        public enum Color { Red, Green }

        public struct Point { }

        public static class Defaults
        {
            public static void M(
                int a,
                int b = 5,
                string? c = null,
                string d = "x\n",
                Color e = Color.Green,
                Color f = (Color)5,
                Color? g = Color.Red,
                double h = 1,
                decimal i = 1.5m,
                Point j = default,
                int? k = null,
                System.Threading.CancellationToken l = default,
                float m = float.NaN,
                char n = 'q',
                byte o = 1,
                long p = -1)
            {
            }

            public static void G<T, TClass>(T x = default!, TClass? y = null) where TClass : class
            {
            }
        }
        """;

    [Fact]
    public void GetDefaultValueExpressionOfEachParameter()
    {
        var compilation = TestCompilation.Create(DefaultSource);
        var type = compilation.GetTypeByMetadataName("Defaults")!;
        var parameters = type.GetMembers("M").OfType<IMethodSymbol>().Single().Parameters;
        var generic = type.GetMembers("G").OfType<IMethodSymbol>().Single().Parameters;

        Assert.Equal(
            [
                null, "5", "null", "\"x\\n\"", "global::Color.Green", "(global::Color)(5)", "global::Color.Red", "1d", "1.5m",
                "default(global::Point)", "null", "default(global::System.Threading.CancellationToken)", "float.NaN", "'q'", "(byte)1", "-1L"
            ],
            parameters.Select(static x => x.GetDefaultValueExpression()));
        Assert.Equal(["default(T)", "null"], generic.Select(static x => x.GetDefaultValueExpression()));
    }

    [Fact]
    public void DefaultValueExpressionsCompile()
    {
        var compilation = TestCompilation.Create(DefaultSource);
        var method = compilation.GetTypeByMetadataName("Defaults")!.GetMembers("M").OfType<IMethodSymbol>().Single();
        var arguments = method.Parameters.Select(static x => x.GetDefaultValueExpression() ?? "0");
        var source = $"public static class Caller {{ public static void Call() => global::Defaults.M({String.Join(", ", arguments)}); }}";

        Assert.Empty(TestCompilation.GetProblems(TestCompilation.AddSource(compilation, source)));
    }
}
