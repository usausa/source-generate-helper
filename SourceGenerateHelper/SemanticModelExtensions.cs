namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class SemanticModelExtensions
{
    public static bool IsImplicitlyConvertible(this SemanticModel semanticModel, int position, string expression, ITypeSymbol type) =>
        semanticModel.ClassifyConversion(position, SyntaxFactory.ParseExpression(expression), type).IsImplicit;

    public static ISymbol? LookupMember(this SemanticModel semanticModel, int position, ITypeSymbol type, string name)
    {
        var symbols = semanticModel.LookupSymbols(position, type, name);
        return symbols.Length == 1 ? symbols[0] : null;
    }

    public static IReadOnlyList<IPropertySymbol> LookupInstanceProperties(this SemanticModel semanticModel, int position, ITypeSymbol type)
    {
        var properties = new List<IPropertySymbol>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var symbol in semanticModel.LookupSymbols(position, type))
        {
            if ((symbol is IPropertySymbol { IsStatic: false, IsIndexer: false }) &&
                names.Add(symbol.Name) &&
                (semanticModel.LookupMember(position, type, symbol.Name) is IPropertySymbol { IsStatic: false } property))
            {
                properties.Add(property);
            }
        }

        return properties;
    }
}
