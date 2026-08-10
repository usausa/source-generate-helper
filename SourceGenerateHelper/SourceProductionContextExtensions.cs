namespace SourceGenerateHelper;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

public static class SourceProductionContextExtensions
{
    public static void AddSource(this SourceProductionContext context, string hintName, SourceBuilder builder) =>
        context.AddSource(hintName, SourceText.From(builder.ToString(), Encoding.UTF8));
}
