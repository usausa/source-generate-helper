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
        var descriptor = CreateDescriptor();
        var properties = ImmutableDictionary<string, string?>.Empty.Add("Key", "Value");
        var info = new DiagnosticInfo(descriptor, (Location?)null, properties);
        var diagnostic = info.ToDiagnostic();
        Assert.Equal("Value", diagnostic.Properties["Key"]);
    }

    [Fact]
    public void NoPropertiesResultsInEmptyDictionary()
    {
        var descriptor = CreateDescriptor();
        var info = new DiagnosticInfo(descriptor, (Location?)null);
        var diagnostic = info.ToDiagnostic();
        Assert.Empty(diagnostic.Properties);
    }

    // ------------------------------------------------------------------
    // MessageArgs
    // ------------------------------------------------------------------

    [Fact]
    public void MessageArgsAreRendered()
    {
        var descriptor = CreateDescriptor();
        var info = new DiagnosticInfo(descriptor, (Location?)null, "abc");
        var diagnostic = info.ToDiagnostic();
        Assert.Equal("Message abc", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    // ------------------------------------------------------------------
    // Location
    // ------------------------------------------------------------------

    [Fact]
    public void LocationIsPreserved()
    {
        var descriptor = CreateDescriptor();
        var (tree, classLocation) = CreateTreeWithClassLocation();
        var info = new DiagnosticInfo(descriptor, classLocation);
        var diagnostic = info.ToDiagnostic();
        Assert.Equal(classLocation.SourceSpan, diagnostic.Location.SourceSpan);
        Assert.Equal(tree.FilePath, diagnostic.Location.GetLineSpan().Path);
    }

    [Fact]
    public void NullLocationResultsInLocationNone()
    {
        var descriptor = CreateDescriptor();
        var info = new DiagnosticInfo(descriptor, (Location?)null);
        var diagnostic = info.ToDiagnostic();
        Assert.Equal(Location.None, diagnostic.Location);
    }

    // ------------------------------------------------------------------
    // LocationInfo.CreateFrom
    // ------------------------------------------------------------------

    [Fact]
    public void CreateFromLocationNoneReturnsNull()
    {
        var result = LocationInfo.CreateFrom(Location.None);
        Assert.Null(result);
    }

    // ------------------------------------------------------------------
    // Record equality
    // ------------------------------------------------------------------

    [Fact]
    public void SameDescriptorAndMessageArgsAreEqual()
    {
        var descriptor = CreateDescriptor();
        var a = new DiagnosticInfo(descriptor, (Location?)null, "x");
        var b = new DiagnosticInfo(descriptor, (Location?)null, "x");
        Assert.Equal(a, b);
    }

    [Fact]
    public void SharedPropertiesInstanceAreEqual()
    {
        var descriptor = CreateDescriptor();
        var properties = ImmutableDictionary<string, string?>.Empty.Add("K", "V");
        var a = new DiagnosticInfo(descriptor, (Location?)null, properties);
        var b = new DiagnosticInfo(descriptor, (Location?)null, properties);
        Assert.Equal(a, b);
    }

    [Fact]
    public void SeparatePropertiesInstancesWithSameContentAreNotEqual()
    {
        var descriptor = CreateDescriptor();
        var propertiesA = ImmutableDictionary<string, string?>.Empty.Add("K", "V");
        var propertiesB = ImmutableDictionary<string, string?>.Empty.Add("K", "V");
        var a = new DiagnosticInfo(descriptor, (Location?)null, propertiesA);
        var b = new DiagnosticInfo(descriptor, (Location?)null, propertiesB);
        Assert.NotEqual(a, b);
    }
}
