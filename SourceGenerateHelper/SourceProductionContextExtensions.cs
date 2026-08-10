namespace SourceGenerateHelper;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

public static class SourceProductionContextExtensions
{
    // The exact call every generator writes to emit a SourceBuilder.
    // The encoding is not optional: without one Roslyn cannot embed the generated file in the PDB,
    // so the source is unavailable when stepping through generated code.
    public static void AddSource(this SourceProductionContext context, string hintName, SourceBuilder builder) =>
        context.AddSource(hintName, SourceText.From(builder.ToString(), Encoding.UTF8));
}
