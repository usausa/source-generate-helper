namespace SourceGeneratorHelper;

using Microsoft.CodeAnalysis;

public static class SymbolExtensions
{
    // ------------------------------------------------------------
    // Namespace
    // ------------------------------------------------------------

    public static IEnumerable<INamedTypeSymbol> GetTypeMembersRecursive(this INamespaceSymbol namespaceSymbol)
    {
        foreach (var typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            yield return typeSymbol;
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var typeSymbol in GetTypeMembersRecursive(nestedNamespace))
            {
                yield return typeSymbol;
            }
        }
    }

    public static IEnumerable<INamedTypeSymbol> GetTypeMembersRecursive(this INamespaceSymbol namespaceSymbol, Func<INamedTypeSymbol, bool> predicate)
    {
        foreach (var typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            if (predicate(typeSymbol))
            {
                yield return typeSymbol;
            }
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            foreach (var typeSymbol in GetTypeMembersRecursive(nestedNamespace, predicate))
            {
                yield return typeSymbol;
            }
        }
    }
}
