namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public static class IncrementalGeneratorInitializationContextExtensions
{
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataNameWithOptions<T>(
        this IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName,
        Func<SyntaxNode, CancellationToken, bool> predicate,
        Func<GeneratorAttributeSyntaxContext, AnalyzerConfigOptions, CancellationToken, T> transform)
    {
        var syntaxContext = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName,
            predicate,
            static (context, _) => context);

        var configOptions = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => provider.GlobalOptions);

        return syntaxContext.Combine(configOptions).Select((input, token) => transform(input.Left, input.Right, token));
    }
}
