namespace SourceGenerateHelper.Tests;

using System.Collections.Immutable;
using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class TestCompilation
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> References = new(static () =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(static x => x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(static x => (MetadataReference)MetadataReference.CreateFromFile(x))
            .ToImmutableArray());

    public static CSharpParseOptions ParseOptions { get; } = new(LanguageVersion.Preview);

    public static CSharpCompilation Create(params string[] sources) =>
        CSharpCompilation.Create(
            "TestAssembly",
            sources.Select(static (x, i) => CSharpSyntaxTree.ParseText(x, ParseOptions, $"Source{i}.cs")),
            References.Value,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                allowUnsafe: true,
                warningLevel: 9999));

    public static SyntaxTree Parse(string source, string path = "") =>
        CSharpSyntaxTree.ParseText(source, ParseOptions, path);

    // A compilation takes trees of one language version
    public static Compilation AddSource(Compilation compilation, string source, string path = "") =>
        compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, (CSharpParseOptions?)compilation.SyntaxTrees.FirstOrDefault()?.Options ?? ParseOptions, path));

    public static IReadOnlyList<Diagnostic> GetProblems(Compilation compilation) =>
        compilation.GetDiagnostics().Where(static x => x.Severity >= DiagnosticSeverity.Warning).ToList();

    public static Assembly Load(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, String.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }

    public static int PositionOf(SyntaxTree tree, string marker) =>
        tree.ToString().IndexOf(marker, StringComparison.Ordinal);
}
