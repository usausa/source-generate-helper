namespace SourceGenerateHelper.Tests;

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public sealed class DiagnosticInfoTest
{
    private static DiagnosticDescriptor CreateDescriptor() =>
        new("TST0001", "Title", "Message {0}", "Test", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static (CSharpSyntaxTree Tree, Location ClassLocation) CreateTreeWithClassLocation()
    {
        var tree = (CSharpSyntaxTree)CSharpSyntaxTree.ParseText(
            "public class Foo { }",
            path: "Test.cs");
        var root = tree.GetRoot();
        var classDecl = root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();
        return (tree, classDecl.GetLocation());
    }

    // ------------------------------------------------------------------
    // Properties
    // ------------------------------------------------------------------

    [Fact]
    public void PropertiesAreAttachedToDiagnostic()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var properties = ImmutableDictionary<string, string?>.Empty.Add("Key", "Value");
        var info = new DiagnosticInfo(descriptor, (Location?)null, properties);

        // Act
        var diagnostic = info.ToDiagnostic();

        // Assert
        Assert.Equal("Value", diagnostic.Properties["Key"]);
    }

    [Fact]
    public void NoPropertiesResultsInEmptyDictionary()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var info = new DiagnosticInfo(descriptor, (Location?)null);

        // Act
        var diagnostic = info.ToDiagnostic();

        // Assert
        Assert.Empty(diagnostic.Properties);
    }

    // ------------------------------------------------------------------
    // MessageArgs
    // ------------------------------------------------------------------

    [Fact]
    public void MessageArgsAreRendered()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var info = new DiagnosticInfo(descriptor, (Location?)null, "abc");

        // Act
        var diagnostic = info.ToDiagnostic();

        // Assert
        Assert.Equal("Message abc", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    // ------------------------------------------------------------------
    // Location
    // ------------------------------------------------------------------

    [Fact]
    public void LocationIsPreserved()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var (tree, classLocation) = CreateTreeWithClassLocation();
        var info = new DiagnosticInfo(descriptor, classLocation);

        // Act
        var diagnostic = info.ToDiagnostic();

        // Assert
        Assert.Equal(classLocation.SourceSpan, diagnostic.Location.SourceSpan);
        Assert.Equal(tree.FilePath, diagnostic.Location.GetLineSpan().Path);
    }

    [Fact]
    public void NullLocationResultsInLocationNone()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var info = new DiagnosticInfo(descriptor, (Location?)null);

        // Act
        var diagnostic = info.ToDiagnostic();

        // Assert
        Assert.Equal(Location.None, diagnostic.Location);
    }

    // ------------------------------------------------------------------
    // LocationInfo.CreateFrom
    // ------------------------------------------------------------------

    [Fact]
    public void CreateFromLocationNoneReturnsNull()
    {
        // Act
        var result = LocationInfo.CreateFrom(Location.None);

        // Assert
        Assert.Null(result);
    }

    // ------------------------------------------------------------------
    // Record equality
    // ------------------------------------------------------------------

    [Fact]
    public void SameDescriptorAndMessageArgsAreEqual()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var a = new DiagnosticInfo(descriptor, (Location?)null, "x");
        var b = new DiagnosticInfo(descriptor, (Location?)null, "x");

        // Act & Assert
        Assert.Equal(a, b);
    }

    [Fact]
    public void SharedPropertiesInstanceAreEqual()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var properties = ImmutableDictionary<string, string?>.Empty.Add("K", "V");
        var a = new DiagnosticInfo(descriptor, (Location?)null, properties);
        var b = new DiagnosticInfo(descriptor, (Location?)null, properties);

        // Act & Assert
        Assert.Equal(a, b);
    }

    [Fact]
    public void SeparatePropertiesInstancesWithSameContentAreEqual()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var propertiesA = ImmutableDictionary<string, string?>.Empty.Add("K", "V");
        var propertiesB = ImmutableDictionary<string, string?>.Empty.Add("K", "V");
        var a = new DiagnosticInfo(descriptor, (Location?)null, propertiesA);
        var b = new DiagnosticInfo(descriptor, (Location?)null, propertiesB);

        // Act & Assert
        Assert.Equal(a, b);
    }

    [Fact]
    public void SeparatePropertiesInstancesWithSameContentHaveSameHashCode()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var propertiesA = ImmutableDictionary<string, string?>.Empty.Add("K", "V");
        var propertiesB = ImmutableDictionary<string, string?>.Empty.Add("K", "V");
        var a = new DiagnosticInfo(descriptor, (Location?)null, propertiesA);
        var b = new DiagnosticInfo(descriptor, (Location?)null, propertiesB);

        // Act & Assert
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void DifferentPropertiesContentAreNotEqual()
    {
        // Arrange
        var descriptor = CreateDescriptor();
        var propertiesA = ImmutableDictionary<string, string?>.Empty.Add("K", "V1");
        var propertiesB = ImmutableDictionary<string, string?>.Empty.Add("K", "V2");
        var a = new DiagnosticInfo(descriptor, (Location?)null, propertiesA);
        var b = new DiagnosticInfo(descriptor, (Location?)null, propertiesB);

        // Act & Assert
        Assert.NotEqual(a, b);
    }
}
