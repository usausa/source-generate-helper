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

    // Arity, as IsGenericType is also true for a type nested in a generic one
    public static string GetClassName(this INamedTypeSymbol symbol) =>
        symbol.Arity > 0
            ? $"{CSharpIdentifier.EscapeTypeName(symbol.Name)}<{String.Join(", ", symbol.TypeArguments.Select(static x => CSharpIdentifier.EscapeTypeName(x.Name)))}>"
            : CSharpIdentifier.EscapeTypeName(symbol.Name);

    public static bool IsGenericType(this ITypeSymbol symbol) =>
        symbol is INamedTypeSymbol { IsGenericType: true } or ITypeParameterSymbol;

    public static string GetDeclarationKeyword(this INamedTypeSymbol symbol) =>
        symbol switch
        {
            { TypeKind: TypeKind.Interface } => "interface",
            { IsRecord: true, IsValueType: true } => "record struct",
            { IsRecord: true } => "record",
            { IsValueType: true } => "struct",
            _ => "class"
        };

    public static string GetPartialDeclaration(this INamedTypeSymbol symbol) =>
        "partial " + symbol.GetDeclarationKeyword() + " " + symbol.GetClassName();

    public static IReadOnlyList<INamedTypeSymbol> GetContainingTypes(this INamedTypeSymbol symbol)
    {
        var types = new List<INamedTypeSymbol>();
        for (var type = symbol.ContainingType; type is not null; type = type.ContainingType)
        {
            types.Insert(0, type);
        }

        return types;
    }

    public static bool HasFullyQualifiedMetadataName(this ITypeSymbol? type, string fullyQualifiedMetadataName)
    {
        if ((type is not INamedTypeSymbol named) || (named.TypeKind == TypeKind.Error))
        {
            return false;
        }

        var end = fullyQualifiedMetadataName.Length;
        var current = named;
        while (true)
        {
            if (!EndsWith(fullyQualifiedMetadataName, end, current.MetadataName))
            {
                return false;
            }

            end -= current.MetadataName.Length;
            if (current.ContainingType is null)
            {
                break;
            }

            if ((end == 0) || (fullyQualifiedMetadataName[end - 1] != '+'))
            {
                return false;
            }

            end--;
            current = current.ContainingType;
        }

        return EndsWithNamespace(fullyQualifiedMetadataName, end, current.ContainingNamespace);
    }

    public static bool CanBeNamedInOtherFile(this ITypeSymbol type)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return array.ElementType.CanBeNamedInOtherFile();
            case INamedTypeSymbol named:
                for (var current = named; current is not null; current = current.ContainingType)
                {
                    if ((current.TypeKind == TypeKind.Error) || current.IsFileLocal)
                    {
                        return false;
                    }

                    if (!current.IsUnboundGenericType)
                    {
                        foreach (var argument in current.TypeArguments)
                        {
                            if (!argument.CanBeNamedInOtherFile())
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            default:
                return type.TypeKind is not (TypeKind.Error or TypeKind.Pointer or TypeKind.FunctionPointer);
        }
    }

    // typeof refuses dynamic and a nullable reference type at its top level
    public static string ToTypeOfName(this ITypeSymbol type) =>
        type.TypeKind == TypeKind.Dynamic ? "object" : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

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

    public static bool CanBeNull(this ITypeSymbol type) =>
        type switch
        {
            ITypeParameterSymbol parameter => !parameter.HasValueTypeConstraint && !parameter.HasUnmanagedTypeConstraint,
            { TypeKind: TypeKind.Pointer or TypeKind.FunctionPointer } => false,
            _ => !type.IsValueType || (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        };

    // ------------------------------------------------------------
    // Base
    // ------------------------------------------------------------

    public static bool InheritsFrom(this ITypeSymbol typeSymbol, string baseTypeFullName)
    {
        var byName = (baseTypeFullName.IndexOf('.') >= 0) && (baseTypeFullName.IndexOf('<') < 0) &&
                     !baseTypeFullName.StartsWith("global::", StringComparison.Ordinal);
        for (var current = typeSymbol; current is not null; current = current.BaseType)
        {
            if (byName
                ? HasFullName(current, baseTypeFullName)
                : (current.ToDisplayString() == baseTypeFullName) ||
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

    public static bool IsImplementsInterfaceByName(this ITypeSymbol typeSymbol, string metadataName)
    {
        var qualified = metadataName.IndexOf('.') >= 0;
        var display = metadataName.IndexOf('<') >= 0;
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            var definition = iface.OriginalDefinition;
            if (qualified
                ? definition.HasFullyQualifiedMetadataName(metadataName) ||
                  HasFullName(definition, metadataName) ||
                  (display && ((definition.ToDisplayString() == metadataName) ||
                               (definition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == $"global::{metadataName}")))
                : definition.MetadataName == metadataName)
            {
                return true;
            }
        }

        return false;
    }

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
            if (namedType.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                return namedType.TypeArguments[0];
            }

            foreach (var iface in namedType.AllInterfaces)
            {
                if (iface.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
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

    public static IReadOnlyList<IPropertySymbol> GetAllPublicProperties(this ITypeSymbol type)
    {
        var properties = new List<IPropertySymbol>();
        var currentType = type;

        while (currentType is not null)
        {
            properties.AddRange(currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(static p => !p.IsStatic && (p.DeclaredAccessibility == Accessibility.Public)));

            currentType = currentType.BaseType;
        }

        return properties;
    }

    // ------------------------------------------------------------
    // Attribute
    // ------------------------------------------------------------

    public static AttributeData? FindAttribute(this ISymbol symbol, string fullyQualifiedMetadataName)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass.HasFullyQualifiedMetadataName(fullyQualifiedMetadataName))
            {
                return attribute;
            }
        }

        return null;
    }

    public static bool HasAttribute(this ISymbol symbol, string fullyQualifiedMetadataName) =>
        symbol.FindAttribute(fullyQualifiedMetadataName) is not null;

    public static bool IsObsolete(this ISymbol symbol) =>
        symbol.IsObsolete(out _);

    public static bool IsObsolete(this ISymbol symbol, out bool isError)
    {
        var attribute = symbol.FindAttribute("System.ObsoleteAttribute");
        isError = (attribute is not null) && attribute.TryGetConstructorArgument<bool>(1, out var error) && error;
        return attribute is not null;
    }

    // ------------------------------------------------------------
    // Parameter
    // ------------------------------------------------------------

    public static string? GetDefaultValueExpression(this IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
        {
            return null;
        }

        var type = parameter.Type;
        var value = parameter.ExplicitDefaultValue;
        if (value is null)
        {
            return type.CanBeNull() && (type is not ITypeParameterSymbol { IsReferenceType: false })
                ? "null"
                : "default(" + type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable) + ")";
        }

        var valueType = type.GetUnderlyingType();
        return valueType.TypeKind == TypeKind.Enum ? CSharpLiteral.FormatEnum(valueType, value) : CSharpLiteral.Format(value);
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

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static bool EndsWith(string text, int end, string value) =>
        (end >= value.Length) && (String.CompareOrdinal(text, end - value.Length, value, 0, value.Length) == 0);

    private static bool EndsWithNamespace(string text, int end, INamespaceSymbol? ns)
    {
        for (var current = ns; (current is not null) && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            if ((end == 0) || (text[end - 1] != '.') || !EndsWith(text, end - 1, current.Name))
            {
                return false;
            }

            end -= current.Name.Length + 1;
        }

        return end == 0;
    }

    private static bool HasFullName(ITypeSymbol type, string name)
    {
        if ((type is not INamedTypeSymbol named) || (named.TypeKind == TypeKind.Error))
        {
            return false;
        }

        var end = name.Length;
        var current = named;
        while (true)
        {
            if ((current.Arity > 0) || !EndsWith(name, end, current.Name))
            {
                return false;
            }

            end -= current.Name.Length;
            if (current.ContainingType is null)
            {
                break;
            }

            if ((end == 0) || (name[end - 1] != '.'))
            {
                return false;
            }

            end--;
            current = current.ContainingType;
        }

        return EndsWithNamespace(name, end, current.ContainingNamespace);
    }
}
