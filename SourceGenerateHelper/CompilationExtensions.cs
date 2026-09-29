namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis;

public static class CompilationExtensions
{
    public static bool HasIdentityConversion(this Compilation compilation, ITypeSymbol type, ITypeSymbol other) =>
        SymbolEqualityComparer.Default.Equals(type, other) ||
        (MayConvertByIdentity(type, other) && compilation.ClassifyCommonConversion(type, other).IsIdentity);

    private static bool MayConvertByIdentity(ITypeSymbol type, ITypeSymbol other) =>
        (type.TypeKind == TypeKind.Dynamic) || (other.TypeKind == TypeKind.Dynamic) ||
        ((type.TypeKind == TypeKind.Array) && (other.TypeKind == TypeKind.Array)) ||
        ((type is INamedTypeSymbol named) && (other is INamedTypeSymbol otherNamed) &&
         (named.IsTupleType || otherNamed.IsTupleType || named.IsNativeIntegerType || otherNamed.IsNativeIntegerType ||
          SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, otherNamed.OriginalDefinition)));
}
