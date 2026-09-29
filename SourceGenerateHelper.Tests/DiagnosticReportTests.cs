namespace SourceGenerateHelper.Tests;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SourceGenerateHelper.Testing;

public sealed class DiagnosticReportTests
{
    private static DiagnosticDescriptor Warning { get; } = new(
        id: "TST0101",
        title: "Warning",
        messageFormat: "Warning at {0}",
        category: "Test",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static DiagnosticDescriptor Error { get; } = new(
        id: "TST0102",
        title: "Error",
        messageFormat: "Error at {0}",
        category: "Test",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    private const string AttributeSource = "public sealed class MarkerAttribute : System.Attribute { }\n";

    private const string SuppressedSource =
        AttributeSource +
        """
        #pragma warning disable TST0101, TST0102
        [Marker] public class Target { }
        #pragma warning restore TST0101, TST0102
        """;

    // ------------------------------------------------------------
    // Location
    // ------------------------------------------------------------

    private static (SyntaxTree Tree, LocationInfo Info) CreateLocation()
    {
        var tree = TestCompilation.Parse("public class Foo { }", "Test.cs");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        return (tree, LocationInfo.CreateFrom(node.Identifier.GetLocation())!);
    }

    [Fact]
    public void LocationInTreeGivenIsInSource()
    {
        var (tree, info) = CreateLocation();

        var location = info.ToLocation([TestCompilation.Parse("class Other { }", "Other.cs"), tree]);

        Assert.Equal(LocationKind.SourceFile, location.Kind);
        Assert.Same(tree, location.SourceTree);
        Assert.Equal(info.TextSpan, location.SourceSpan);
    }

    [Fact]
    public void LocationWithoutItsTreeIsExternal()
    {
        var (_, info) = CreateLocation();

        Assert.Equal(LocationKind.ExternalFile, info.ToLocation().Kind);
        Assert.Equal(LocationKind.ExternalFile, info.ToLocation([TestCompilation.Parse("class Other { }", "Other.cs")]).Kind);
        Assert.Equal(LocationKind.ExternalFile, info.ToLocation(default).Kind);
        Assert.Equal(LocationKind.ExternalFile, info.ToLocation([]).Kind);
    }

    [Fact]
    public void DiagnosticInTreeGivenIsInSource()
    {
        var (tree, info) = CreateLocation();

        var diagnostic = new DiagnosticInfo(Warning, info, "x").ToDiagnostic([tree]);

        Assert.Same(tree, diagnostic.Location.SourceTree);
        Assert.Equal("Warning at x", diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture));
    }

    // ------------------------------------------------------------
    // Suppression
    // ------------------------------------------------------------

    [Fact]
    public void PragmaSuppressesWarningInTreeButNotError()
    {
        var diagnostics = new GeneratorTestRunner(new TreeReportingGenerator()).Run(SuppressedSource).GeneratorDiagnostics;

        Assert.True(diagnostics.Single(static x => x.Id == "TST0101").IsSuppressed);
        Assert.False(diagnostics.Single(static x => x.Id == "TST0102").IsSuppressed);
    }

    [Fact]
    public void PragmaDoesNotSuppressWarningOutsideTree()
    {
        var diagnostics = new GeneratorTestRunner(new ExternalReportingGenerator()).Run(SuppressedSource).GeneratorDiagnostics;

        Assert.False(diagnostics.Single(static x => x.Id == "TST0101").IsSuppressed);
    }

    [Fact]
    public void SeverityOptionDoesNotSuppressError()
    {
        var (driver, compilation) = new GeneratorTestRunner(new TreeReportingGenerator()).CreateDriver(AttributeSource + "[Marker] public class Target { }");
        var options = compilation.Options.WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic>
        {
            ["TST0101"] = ReportDiagnostic.Suppress,
            ["TST0102"] = ReportDiagnostic.Suppress
        });

        var diagnostics = driver.RunGenerators(compilation.WithOptions(options), TestContext.Current.CancellationToken).GetRunResult().Diagnostics;

        Assert.DoesNotContain(diagnostics, static x => x.Id == "TST0101");
        Assert.Contains(diagnostics, static x => x.Id == "TST0102");
    }

    [Fact]
    public void ProblemsLeaveOutSuppressedWarning()
    {
        var problems = new GeneratorTestRunner(new TreeReportingGenerator()).GetProblems(SuppressedSource);

        Assert.Equal(["TST0102"], problems.Select(static x => x.Id));
    }

    [Fact]
    public void ReportStaysCachedWhenOtherFileIsAdded()
    {
        var result = new GeneratorTestRunner(new TreeReportingGenerator())
            .WithTracking()
            .RunIncremental(AttributeSource + "[Marker] public class Target { }", "public class Unrelated { }");

        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    // ------------------------------------------------------------
    // Generators
    // ------------------------------------------------------------

    internal sealed class TreeReportingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var locations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "MarkerAttribute",
                    static (_, _) => true,
                    static (syntaxContext, _) => LocationInfo.CreateFrom(((ClassDeclarationSyntax)syntaxContext.TargetNode).Identifier.GetLocation())!)
                .Collect();
            var trees = context.ForAttributeWithMetadataNameSyntaxTrees("MarkerAttribute", static (_, _) => true);

            context.RegisterSourceOutput(locations.Combine(trees), static (production, pair) =>
                production.ReportDiagnostics(Report(pair.Left), pair.Right));
        }
    }

    internal sealed class ExternalReportingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var locations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "MarkerAttribute",
                    static (_, _) => true,
                    static (syntaxContext, _) => LocationInfo.CreateFrom(((ClassDeclarationSyntax)syntaxContext.TargetNode).Identifier.GetLocation())!)
                .Collect();

            context.RegisterSourceOutput(locations, static (production, items) =>
                production.ReportDiagnostics(Report(items)));
        }
    }

    private static IEnumerable<DiagnosticInfo> Report(ImmutableArray<LocationInfo> locations) =>
        locations.SelectMany(static x => new[] { new DiagnosticInfo(Warning, x, "x"), new DiagnosticInfo(Error, x, "x") });
}
