namespace SourceGenerateHelper.Tests;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed class SyntaxExtensionsTest
{
    private static BaseTypeDeclarationSyntax GetFirstType(string source)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        return tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>().First();
    }

    [Fact]
    public void NoNamespaceReturnsEmpty()
    {
        // Arrange
        var syntax = GetFirstType("class C { }");

        // Act & Assert
        Assert.Empty(syntax.GetNamespace());
    }

    [Fact]
    public void FileScopedNamespace()
    {
        // Arrange
        var syntax = GetFirstType("namespace Foo; class C { }");

        // Act & Assert
        Assert.Equal("Foo", syntax.GetNamespace());
    }

    [Fact]
    public void FileScopedNamespaceDotted()
    {
        // Arrange
        var syntax = GetFirstType("namespace A.B.C; class C { }");

        // Act & Assert
        Assert.Equal("A.B.C", syntax.GetNamespace());
    }

    [Fact]
    public void BlockScopedNamespace()
    {
        // Arrange
        var syntax = GetFirstType("namespace Foo { class C { } }");

        // Act & Assert
        Assert.Equal("Foo", syntax.GetNamespace());
    }

    [Fact]
    public void NestedBlockScopedNamespaceTwoLevels()
    {
        // Arrange
        var syntax = GetFirstType("namespace Outer { namespace Inner { class C { } } }");

        // Act & Assert
        Assert.Equal("Outer.Inner", syntax.GetNamespace());
    }

    [Fact]
    public void NestedBlockScopedNamespaceThreeLevels()
    {
        // Arrange
        var syntax = GetFirstType("namespace A { namespace B { namespace C { class X { } } } }");

        // Act & Assert
        Assert.Equal("A.B.C", syntax.GetNamespace());
    }

    [Fact]
    public void NestedBlockScopedNamespaceDottedOuter()
    {
        // Arrange
        var syntax = GetFirstType("namespace Outer.Mid { namespace Inner { class C { } } }");

        // Act & Assert
        Assert.Equal("Outer.Mid.Inner", syntax.GetNamespace());
    }
}
