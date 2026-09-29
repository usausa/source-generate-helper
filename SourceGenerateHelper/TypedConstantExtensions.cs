namespace SourceGenerateHelper;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class TypedConstantExtensions
{
    // ------------------------------------------------------------
    // Value
    // ------------------------------------------------------------

    public static bool TryGetValue<T>(this TypedConstant constant, [MaybeNullWhen(false)] out T value)
    {
        if ((constant.Kind is not (TypedConstantKind.Error or TypedConstantKind.Array)) && !constant.IsNull &&
            (constant.Value is not ITypeSymbol { TypeKind: TypeKind.Error }))
        {
            if (constant.Value is T typed)
            {
                value = typed;
                return true;
            }

            if (typeof(T).IsEnum && (constant.Value is { } number) && IsInteger(number))
            {
                value = (T)Enum.ToObject(typeof(T), number);
                return true;
            }
        }

        value = default;
        return false;
    }

    public static bool TryGetValues<T>(this TypedConstant constant, [NotNullWhen(true)] out T[]? values)
    {
        if ((constant.Kind != TypedConstantKind.Array) || constant.IsNull)
        {
            values = null;
            return false;
        }

        var elements = constant.Values;
        var result = new T[elements.Length];
        for (var i = 0; i < elements.Length; i++)
        {
            var element = elements[i];
            if (element.Kind == TypedConstantKind.Error)
            {
                values = null;
                return false;
            }

            if (element.IsNull)
            {
                result[i] = default!;
            }
            else if (element.TryGetValue<T>(out var value))
            {
                result[i] = value;
            }
            else
            {
                values = null;
                return false;
            }
        }

        values = result;
        return true;
    }

    // ------------------------------------------------------------
    // Convert
    // ------------------------------------------------------------

    public static string ToCSharpStringWithPostfix(this TypedConstant constant)
    {
        // Roslyn formats special values as NaN/Infinity, which are not valid C#
        if (constant.Kind == TypedConstantKind.Primitive)
        {
            switch (constant.Value)
            {
                case float f:
                    return CSharpLiteral.FormatSingle(f);
                case double d:
                    return CSharpLiteral.FormatDouble(d);
            }
        }

        var str = constant.ToCSharpString();
        return constant.Type?.SpecialType switch
        {
            SpecialType.System_Int64 => $"{str}L",
            SpecialType.System_UInt32 => $"{str}u",
            SpecialType.System_UInt64 => $"{str}uL",
            SpecialType.System_Single => $"{str}f",
            SpecialType.System_Double => $"{str}d",
            SpecialType.System_Decimal => $"{str}m",
            _ => str
        };
    }

    public static string? ToCSharpExpression(this TypedConstant constant, ITypeSymbol? targetType = null)
    {
        var expression = constant.Kind == TypedConstantKind.Array ? null : MakeExpression(constant, typed: true);
        if (expression is null)
        {
            return null;
        }

        if ((targetType is null) ||
            (constant.Kind != TypedConstantKind.Primitive) ||
            constant.IsNull ||
            (constant.Type is null) ||
            SymbolEqualityComparer.Default.Equals(constant.Type, targetType))
        {
            return expression;
        }

        var targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return expression[0] == '-'
            ? $"({targetTypeName})({expression})"
            : $"({targetTypeName}){expression}";
    }

    public static bool TryToCSharpExpression(
        this TypedConstant constant,
        ITypeSymbol targetType,
        SemanticModel semanticModel,
        int position,
        [NotNullWhen(true)] out string? expression)
    {
        var text = MakeExpression(constant, typed: true);
        if ((text is not null) && semanticModel.IsImplicitlyConvertible(position, text, targetType))
        {
            expression = text;
            return true;
        }

        expression = null;
        return false;
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static bool IsInteger(object? value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong;

    private static string? MakeExpression(TypedConstant constant, bool typed)
    {
        if (constant.Kind == TypedConstantKind.Error)
        {
            return null;
        }

        if (constant.IsNull)
        {
            return "null";
        }

        return constant.Kind switch
        {
            TypedConstantKind.Primitive => typed ? CSharpLiteral.Format(constant.Value) : CSharpLiteral.FormatUntyped(constant.Value),
            TypedConstantKind.Enum => constant.Type is null ? null : CSharpLiteral.FormatEnum(constant.Type, constant.Value),
            TypedConstantKind.Type => (constant.Value is ITypeSymbol type) && type.CanBeNamedInOtherFile() ? "typeof(" + type.ToTypeOfName() + ")" : null,
            TypedConstantKind.Array => MakeArrayExpression(constant),
            _ => null
        };
    }

    private static string? MakeArrayExpression(TypedConstant constant)
    {
        if ((constant.Type is not IArrayTypeSymbol arrayType) || !arrayType.ElementType.CanBeNamedInOtherFile())
        {
            return null;
        }

        var elements = new StringBuilder();
        var hasNull = false;
        foreach (var element in constant.Values)
        {
            var text = MakeExpression(element, typed: !SymbolEqualityComparer.Default.Equals(element.Type, arrayType.ElementType));
            if (text is null)
            {
                return null;
            }

            hasNull |= element.IsNull;
            elements.Append(elements.Length == 0 ? string.Empty : ", ").Append(text);
        }

        var elementType = arrayType.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (hasNull && arrayType.ElementType.IsReferenceType)
        {
            elementType += "?";
        }

        return constant.Values.Length == 0
            ? "new " + elementType + "[0]"
            : "new " + elementType + "[] { " + elements + " }";
    }
}
