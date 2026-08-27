namespace SourceGenerateHelper.Tests;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper.Testing;

public sealed class GeneratorTestRunnerTest
{
    // ------------------------------------------------------------
    // Source
    // ------------------------------------------------------------

    [Fact]
    public void WhenTargetExistsThenSourceIsGenerated()
    {
        var generated = GeneratorTestRunner.For<MarkerGenerator>().GetGeneratedSource(TargetSource);

        Assert.Contains("// generated for Target", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenNoTargetThenGeneratedSourceIsEmpty()
    {
        var generated = GeneratorTestRunner.For<MarkerGenerator>().GetGeneratedSource(AttributeOnly);

        Assert.Equal(String.Empty, generated);
    }

    [Fact]
    public void WhenMultipleTargetsThenEachHintNameIsAvailable()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>().Run(
            AttributeOnly + "[Marker] public class A { }\n[Marker] public class B { }");

        Assert.Equal(2, result.GeneratedSources.Count);
        Assert.Contains("// generated for A", result.GeneratedSource("A.g.cs"), StringComparison.Ordinal);
        Assert.Contains("// generated for B", result.GeneratedSource("B.g.cs"), StringComparison.Ordinal);
        Assert.Contains("// generated for A", result.AllGeneratedText, StringComparison.Ordinal);
        Assert.Contains("// generated for B", result.AllGeneratedText, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Diagnostics
    // ------------------------------------------------------------

    [Fact]
    public void WhenPrefixIsSetThenOnlyMatchingDiagnosticsAreReturned()
    {
        var diagnostics = GeneratorTestRunner.For<MarkerGenerator>()
            .WithDiagnosticPrefix("TST")
            .GetDiagnostics(AttributeOnly + "[Marker] public class Invalid { }");

        Assert.Equal("TST0001", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public void WhenPrefixDoesNotMatchThenDiagnosticsAreEmpty()
    {
        var diagnostics = GeneratorTestRunner.For<MarkerGenerator>()
            .WithDiagnosticPrefix("XXX")
            .GetDiagnostics(AttributeOnly + "[Marker] public class Invalid { }");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void WhenNoPrefixIsSetThenAllGeneratorDiagnosticsAreReturned()
    {
        var diagnostics = GeneratorTestRunner.For<MarkerGenerator>()
            .GetDiagnostics(AttributeOnly + "[Marker] public class Invalid { }");

        Assert.Equal("TST0001", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public void WhenGetDiagnosticsAllThenCompilationDiagnosticsAreIncluded()
    {
        var diagnostics = GeneratorTestRunner.For<MarkerGenerator>()
            .GetDiagnosticsAll(AttributeOnly + "[Marker] public class Target { } public class Broken { int x = ; }");

        Assert.Contains(diagnostics, static x => x.Severity == DiagnosticSeverity.Error);
    }

    // ------------------------------------------------------------
    // Compile
    // ------------------------------------------------------------

    [Fact]
    public void WhenVerifyCompilesAndOutputIsBrokenThenThrows()
    {
        var runner = new GeneratorTestRunner(new BrokenGenerator()).VerifyCompiles();

        var exception = Assert.Throws<InvalidOperationException>(() => runner.Run(AttributeOnly + "[Marker] public class Target { }"));

        Assert.Contains("Generated code does not compile", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenVerifyCompilesIsOffThenBrokenOutputIsReported()
    {
        var result = new GeneratorTestRunner(new BrokenGenerator()).Run(AttributeOnly + "[Marker] public class Target { }");

        Assert.NotEmpty(result.CompilationErrors);
    }

    [Fact]
    public void WhenVerifyCompilesAndOutputIsValidThenNoThrow()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>()
            .VerifyCompiles()
            .Run(TargetSource);

        Assert.Empty(result.CompilationErrors);
    }

    // ------------------------------------------------------------
    // Options
    // ------------------------------------------------------------

    [Fact]
    public void WhenGlobalOptionIsSuppliedThenGeneratorReadsIt()
    {
        var generated = new GeneratorTestRunner(new OptionGenerator())
            .WithGlobalOption("build_property.TestValue", "configured")
            .GetGeneratedSource("public class Any { }");

        Assert.Contains("// value=configured", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenGlobalOptionIsMissingThenGeneratorSeesDefault()
    {
        var generated = new GeneratorTestRunner(new OptionGenerator()).GetGeneratedSource("public class Any { }");

        Assert.Contains("// value=none", generated, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Text
    // ------------------------------------------------------------

    [Fact]
    public void WhenAdditionalTextIsSuppliedThenGeneratorReadsIt()
    {
        var generated = new GeneratorTestRunner(new AdditionalTextGenerator())
            .WithAdditionalText("/proj/Data/sample.txt", "payload")
            .GetGeneratedSource("public class Any { }");

        Assert.Contains("// sample.txt=payload", generated, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Multiple
    // ------------------------------------------------------------

    [Fact]
    public void WhenTwoGeneratorsAreRegisteredThenBothRun()
    {
        var result = new GeneratorTestRunner(new MarkerGenerator())
            .Add(new OptionGenerator())
            .Run(TargetSource);

        Assert.Contains("// generated for Target", result.AllGeneratedText, StringComparison.Ordinal);
        Assert.Contains("// value=none", result.AllGeneratedText, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Output
    // ------------------------------------------------------------

    [Fact]
    public void WhenConsoleApplicationAndEntryPointExistsThenNoCompilationError()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .Run(AttributeOnly + "[Marker] public class Target { }\npublic static class Program { public static void Main() { } }");

        Assert.Empty(result.CompilationErrors);
    }

    [Fact]
    public void WhenConsoleApplicationAndNoEntryPointThenCompilationError()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>()
            .WithOutputKind(OutputKind.ConsoleApplication)
            .Run(TargetSource);

        Assert.Contains(result.CompilationErrors, static x => x.Id == "CS5001");
    }

    // ------------------------------------------------------------
    // Hint name
    // ------------------------------------------------------------

    [Fact]
    public void WhenHintNameIsMissingThenFindReturnsNull()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>().Run(TargetSource);

        Assert.Null(result.FindGeneratedSource("Missing.g.cs"));
        Assert.Equal(String.Empty, result.GeneratedSource("Missing.g.cs"));
    }

    [Fact]
    public void WhenHintNameExistsThenFindReturnsText()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>().Run(TargetSource);

        Assert.Contains("// generated for Target", result.FindGeneratedSource("Target.g.cs")!, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------
    // Tracking
    // ------------------------------------------------------------

    [Fact]
    public void WhenTrackingIsEnabledThenStepsAreRecorded()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>()
            .WithTracking()
            .Run(TargetSource);

        Assert.NotEmpty(result.DriverResult.Results[0].TrackedSteps);
    }

    [Fact]
    public void WhenTrackingIsDisabledThenStepsAreEmpty()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>().Run(TargetSource);

        Assert.Empty(result.DriverResult.Results[0].TrackedSteps);
    }

    // ------------------------------------------------------------
    // Incremental
    // ------------------------------------------------------------

    [Fact]
    public void WhenAddedSourceIsUnrelatedThenOutputIsNotChanged()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>()
            .WithTracking()
            .RunIncremental(TargetSource, UnrelatedSource);

        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void WhenAddedSourceIsTargetThenOutputIsChanged()
    {
        var result = GeneratorTestRunner.For<MarkerGenerator>()
            .WithTracking()
            .RunIncremental(TargetSource, "[Marker] public class Added { }");

        Assert.NotEqual(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void WhenTrackingIsDisabledThenRunIncrementalThrows()
    {
        var runner = GeneratorTestRunner.For<MarkerGenerator>();

        Assert.Throws<InvalidOperationException>(() => runner.RunIncremental(TargetSource, UnrelatedSource));
    }

    [Theory]
    [InlineData(IncrementalStepRunReason.New, true)]
    [InlineData(IncrementalStepRunReason.Modified, true)]
    [InlineData(IncrementalStepRunReason.Removed, true)]
    [InlineData(IncrementalStepRunReason.Cached, false)]
    [InlineData(IncrementalStepRunReason.Unchanged, false)]
    public void WhenReasonIsGivenThenIsChangedReflectsIt(IncrementalStepRunReason reason, bool expected)
    {
        Assert.Equal(expected, reason.IsChanged());
    }

    // ------------------------------------------------------------
    // Fixtures
    // ------------------------------------------------------------

    private const string AttributeOnly = "public sealed class MarkerAttribute : System.Attribute { }\n";

    private const string TargetSource = AttributeOnly + "[Marker] public class Target { }";

    private const string UnrelatedSource = "public class Unrelated { }";

    internal sealed class MarkerGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor InvalidName = new(
            id: "TST0001",
            title: "Invalid name",
            messageFormat: "Type must not be named Invalid: {0}",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "MarkerAttribute",
                    static (_, _) => true,
                    static (syntaxContext, _) => syntaxContext.TargetSymbol.Name)
                .WithTrackingName("MarkerTarget");

            context.RegisterSourceOutput(provider, static (production, name) =>
            {
                if (name == "Invalid")
                {
                    production.ReportDiagnostic(Diagnostic.Create(InvalidName, Location.None, name));
                    return;
                }

                production.AddSource($"{name}.g.cs", SourceText.From($"// generated for {name}", Encoding.UTF8));
            });
        }
    }

    internal sealed class BrokenGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    "MarkerAttribute",
                    static (_, _) => true,
                    static (syntaxContext, _) => syntaxContext.TargetSymbol.Name);

            context.RegisterSourceOutput(provider, static (production, name) =>
                production.AddSource($"{name}.g.cs", SourceText.From("public class Broken { this is not C# }", Encoding.UTF8)));
        }
    }

    internal sealed class OptionGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.AnalyzerConfigOptionsProvider.Select(static (options, _) =>
                options.GlobalOptions.TryGetValue("build_property.TestValue", out var value) ? value : "none");

            context.RegisterSourceOutput(provider, static (production, value) =>
                production.AddSource("Option.g.cs", SourceText.From($"// value={value}", Encoding.UTF8)));
        }
    }

    internal sealed class AdditionalTextGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.AdditionalTextsProvider.Select(static (text, token) =>
                (Name: Path.GetFileName(text.Path), Content: text.GetText(token)?.ToString() ?? String.Empty));

            context.RegisterSourceOutput(provider, static (production, item) =>
                production.AddSource($"{item.Name}.g.cs", SourceText.From($"// {item.Name}={item.Content}", Encoding.UTF8)));
        }
    }
}
