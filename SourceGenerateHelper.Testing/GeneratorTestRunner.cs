namespace SourceGenerateHelper.Testing;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public sealed class GeneratorTestRunner
{
    private static readonly Lazy<bool> DependenciesLoaded = new(static () =>
    {
        var directory = Path.GetDirectoryName(typeof(GeneratorTestRunner).Assembly.Location);
        if (directory is not null)
        {
            var helper = Path.Combine(directory, "SourceGenerateHelper.dll");
            if (File.Exists(helper))
            {
                Assembly.LoadFrom(helper);
            }
        }

        return true;
    });

    private readonly List<IIncrementalGenerator> generators = [];

    private readonly List<Assembly> referenceAssemblies = [];

    private readonly List<string> diagnosticPrefixes = [];

    private readonly Dictionary<string, string> globalOptions = [];

    private readonly List<AdditionalText> additionalTexts = [];

    private LanguageVersion languageVersion = LanguageVersion.Preview;

    private string assemblyName = "GeneratorTestAssembly";

    private NullableContextOptions nullableContextOptions = NullableContextOptions.Enable;

    private OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary;

    private bool verifyCompiles;

    private bool trackSteps;

    public GeneratorTestRunner(params IIncrementalGenerator[] generators)
    {
        ArgumentNullException.ThrowIfNull(generators);

        this.generators.AddRange(generators);
    }

    public static GeneratorTestRunner For<TGenerator>()
        where TGenerator : IIncrementalGenerator, new()
        => new(new TGenerator());

    // ------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------

    public GeneratorTestRunner Add(IIncrementalGenerator generator)
    {
        generators.Add(generator);
        return this;
    }

    public GeneratorTestRunner WithReference(Assembly assembly)
    {
        referenceAssemblies.Add(assembly);
        return this;
    }

    public GeneratorTestRunner WithDiagnosticPrefix(params string[] prefixes)
    {
        ArgumentNullException.ThrowIfNull(prefixes);

        diagnosticPrefixes.AddRange(prefixes);
        return this;
    }

    public GeneratorTestRunner WithGlobalOption(string key, string value)
    {
        globalOptions[key] = value;
        return this;
    }

    public GeneratorTestRunner WithAdditionalText(string path, string content)
    {
        additionalTexts.Add(new InMemoryAdditionalText(path, content));
        return this;
    }

    public GeneratorTestRunner WithLanguageVersion(LanguageVersion version)
    {
        languageVersion = version;
        return this;
    }

    public GeneratorTestRunner WithAssemblyName(string name)
    {
        assemblyName = name;
        return this;
    }

    public GeneratorTestRunner WithNullableContext(NullableContextOptions options)
    {
        nullableContextOptions = options;
        return this;
    }

    public GeneratorTestRunner WithOutputKind(OutputKind kind)
    {
        outputKind = kind;
        return this;
    }

    public GeneratorTestRunner VerifyCompiles(bool enabled = true)
    {
        verifyCompiles = enabled;
        return this;
    }

    public GeneratorTestRunner WithTracking(bool enabled = true)
    {
        trackSteps = enabled;
        return this;
    }

    // ------------------------------------------------------------
    // Execution
    // ------------------------------------------------------------

    public GeneratorTestResult Run(string source)
    {
        var (driver, compilation) = CreateDriver(source);

        var updated = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        var runResult = updated.GetRunResult();

        var generated = new Dictionary<string, string>(StringComparer.Ordinal);
        var all = new StringBuilder();
        foreach (var generatedSource in runResult.Results.SelectMany(static x => x.GeneratedSources))
        {
            var text = generatedSource.SourceText.ToString();
            generated[generatedSource.HintName] = text;
            all.Append(text).AppendLine();
        }

        var compilationErrors = outputCompilation.GetDiagnostics()
            .Where(static x => x.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (verifyCompiles &&
            !runResult.Diagnostics.Any(static x => x.Severity == DiagnosticSeverity.Error) &&
            (compilationErrors.Count > 0))
        {
            throw new InvalidOperationException(BuildCompileFailureMessage(compilationErrors));
        }

        return new GeneratorTestResult(runResult, updated, outputCompilation, generated, all.ToString(), compilationErrors);
    }

    public (GeneratorDriver Driver, Compilation Compilation) CreateDriver(string source)
    {
        _ = DependenciesLoaded.Value;

        var parseOptions = new CSharpParseOptions(languageVersion);
        var compilation = CSharpCompilation.Create(
            assemblyName: assemblyName,
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references: BuildReferences(),
            options: new CSharpCompilationOptions(outputKind, nullableContextOptions: nullableContextOptions));

        var driver = CSharpGeneratorDriver.Create(
            generators: generators.Select(static x => x.AsSourceGenerator()).ToImmutableArray(),
            additionalTexts: additionalTexts.ToImmutableArray(),
            parseOptions: parseOptions,
            optionsProvider: globalOptions.Count > 0 ? new DictionaryAnalyzerConfigOptionsProvider(globalOptions) : null,
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: trackSteps));

        return (driver, compilation);
    }

    // ------------------------------------------------------------
    // Results
    // ------------------------------------------------------------

    public string GetGeneratedSource(string source) => Run(source).FirstGeneratedSource;

    public IReadOnlyList<Diagnostic> GetDiagnostics(string source) => Run(source).Diagnostics(diagnosticPrefixes);

    public IReadOnlyList<Diagnostic> GetDiagnosticsAll(string source)
    {
        var result = Run(source);
        return [.. result.GeneratorDiagnostics, .. result.OutputCompilation.GetDiagnostics()];
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private List<MetadataReference> BuildReferences()
    {
        var references = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var path in trusted.Split(Path.PathSeparator))
            {
                if (!String.IsNullOrEmpty(path) &&
                    path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                    seen.Add(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        foreach (var assembly in referenceAssemblies)
        {
            if (!String.IsNullOrEmpty(assembly.Location) && seen.Add(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        return references;
    }

    private static string BuildCompileFailureMessage(IEnumerable<Diagnostic> errors)
    {
        var message = new StringBuilder();
        message.AppendLine("Generated code does not compile:");
        foreach (var error in errors)
        {
            message.Append("  ")
                .Append(error.Id)
                .Append(": ")
                .Append(error.GetMessage(System.Globalization.CultureInfo.InvariantCulture))
                .Append(" @ ")
                .Append(error.Location.GetLineSpan().ToString())
                .AppendLine();
        }

        return message.ToString();
    }
}
