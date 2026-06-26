namespace SourceGenerateHelper;

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
            foreach (var typeSymbol in nestedNamespace.GetTypeMembersRecursive())
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
            foreach (var typeSymbol in nestedNamespace.GetTypeMembersRecursive(predicate))
            {
                yield return typeSymbol;
            }
        }
    }

    // ------------------------------------------------------------
    // Type
    // ------------------------------------------------------------

    public static string GetClassName(this INamedTypeSymbol symbol) =>
        symbol.IsGenericType
            ? $"{symbol.Name}<{String.Join(", ", symbol.TypeArguments.Select(static x => x.Name))}>"
            : symbol.Name;

    public static bool IsGenericType(this ITypeSymbol symbol) =>
        symbol is INamedTypeSymbol { IsGenericType: true } or ITypeParameterSymbol;

    // ------------------------------------------------------------
    // Nullable
    // ------------------------------------------------------------

    public static bool IsNullableType(this ITypeSymbol type)
    {
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }

    public static ITypeSymbol GetUnderlyingType(this ITypeSymbol type)
    {
        if ((type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) &&
            (type is INamedTypeSymbol namedType) &&
            (namedType.TypeArguments.Length == 1))
        {
            return namedType.TypeArguments[0];
        }

        if ((type.NullableAnnotation == NullableAnnotation.Annotated) &&
            (type is INamedTypeSymbol refType))
        {
            return refType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        return type;
    }

    // ------------------------------------------------------------
    // Base
    // ------------------------------------------------------------

    public static bool InheritsFrom(this ITypeSymbol typeSymbol, string baseTypeFullName)
    {
        for (var current = typeSymbol; current is not null; current = current.BaseType)
        {
            if ((current.ToDisplayString() == baseTypeFullName) ||
                (current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == $"global::{baseTypeFullName}"))
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------
    // Assignable
    // ------------------------------------------------------------

    public static bool IsAssignableTo(this ITypeSymbol sourceType, ITypeSymbol targetType)
    {
        if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
        {
            return true;
        }

        if (targetType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            var nonNullableTarget = targetType.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            if (SymbolEqualityComparer.Default.Equals(sourceType, nonNullableTarget))
            {
                return true;
            }
        }

        var current = sourceType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, targetType))
            {
                return true;
            }
            current = current.BaseType;
        }

        foreach (var iface in sourceType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, targetType))
            {
                return true;
            }
        }

        return false;
    }

    // ------------------------------------------------------------
    // Interface
    // ------------------------------------------------------------

    public static bool IsImplementGenericInterface(this ITypeSymbol typeSymbol, INamedTypeSymbol genericInterfaceDefinition) =>
        typeSymbol.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, genericInterfaceDefinition));

    public static bool IsImplementsInterfaceByName(this ITypeSymbol typeSymbol, string metadataName) =>
        typeSymbol.AllInterfaces.Any(i =>
            (i.OriginalDefinition.ToDisplayString() == metadataName) ||
            (i.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == $"global::{metadataName}") ||
            (i.OriginalDefinition.MetadataName == metadataName.Split('.').Last()));

    // -------------------------------------------------------
    // Collection
    // -------------------------------------------------------

    public static ITypeSymbol? GetCollectionElementType(this ITypeSymbol collectionType)
    {
        if (collectionType is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        if (collectionType is INamedTypeSymbol { IsGenericType: true } namedType)
        {
            if (namedType.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>")
            {
                return namedType.TypeArguments[0];
            }

            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.IsGenericType &&
                    (iface.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>"))
                {
                    return iface.TypeArguments[0];
                }
            }
        }

        return null;
    }

    // -------------------------------------------------------
    // Property
    // -------------------------------------------------------

    public static List<IPropertySymbol> GetAllPublicProperties(this ITypeSymbol type)
    {
        var properties = new List<IPropertySymbol>();
        var currentType = type;

        while (currentType is not null)
        {
            properties.AddRange(currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(static p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public));

            currentType = currentType.BaseType;
        }

        return properties;
    }

    // ------------------------------------------------------------
    // Numeric
    // ------------------------------------------------------------

    public static bool IsNumericType(this ITypeSymbol type) =>
        type.SpecialType is
            SpecialType.System_Byte or
            SpecialType.System_SByte or
            SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt64;

    // ------------------------------------------------------------
    // Enum
    // ------------------------------------------------------------

    public static ITypeSymbol? GetEnumUnderlyingType(this ITypeSymbol type)
    {
        if ((type is INamedTypeSymbol { IsGenericType: true } nullableType) &&
            (nullableType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) &&
            (nullableType.TypeArguments[0] is INamedTypeSymbol { TypeKind: TypeKind.Enum } innerEnum))
        {
            return innerEnum.EnumUnderlyingType;
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } namedEnum)
        {
            return namedEnum.EnumUnderlyingType;
        }

        return null;
    }
}
