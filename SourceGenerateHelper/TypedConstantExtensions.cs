namespace SourceGenerateHelper;

using System;
using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class TypedConstantExtensions
{
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
                    return FormatSingle(f);
                case double d:
                    return FormatDouble(d);
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
        if (constant.IsNull)
        {
            return "null";
        }

        var expression = MakeExpression(constant);
        if (expression is null)
        {
            return null;
        }

        if ((targetType is null) ||
            (constant.Kind != TypedConstantKind.Primitive) ||
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

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string? MakeExpression(TypedConstant constant) =>
        constant.Kind switch
        {
            TypedConstantKind.Primitive => MakePrimitiveExpression(constant.Value),
            TypedConstantKind.Enum => MakeEnumExpression(constant),
            TypedConstantKind.Type => $"typeof({((ITypeSymbol)constant.Value!).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})",
            _ => null
        };

    private static string? MakePrimitiveExpression(object? value) =>
        value switch
        {
            string s => SymbolDisplay.FormatLiteral(s, true),
            char c => SymbolDisplay.FormatLiteral(c, true),
            bool b => b ? "true" : "false",
            float f => FormatSingle(f),
            double d => FormatDouble(d),
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
            long l => l.ToString(CultureInfo.InvariantCulture) + "L",
            ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "uL",
            uint ui => ui.ToString(CultureInfo.InvariantCulture) + "u",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => null
        };

    private static string MakeEnumExpression(TypedConstant constant)
    {
        var typeName = constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // A combined flags value has no single member name, so a cast is used instead
        foreach (var member in constant.Type.GetMembers())
        {
            if ((member is IFieldSymbol { IsConst: true, HasConstantValue: true } field) &&
                Equals(field.ConstantValue, constant.Value))
            {
                return $"{typeName}.{field.Name}";
            }
        }

        return $"({typeName})({((IFormattable)constant.Value!).ToString(null, CultureInfo.InvariantCulture)})";
    }

    private static string FormatSingle(float value)
    {
        if (Single.IsNaN(value))
        {
            return "float.NaN";
        }

        if (Single.IsPositiveInfinity(value))
        {
            return "float.PositiveInfinity";
        }

        if (Single.IsNegativeInfinity(value))
        {
            return "float.NegativeInfinity";
        }

        return value.ToString("R", CultureInfo.InvariantCulture) + "f";
    }

    private static string FormatDouble(double value)
    {
        if (Double.IsNaN(value))
        {
            return "double.NaN";
        }

        if (Double.IsPositiveInfinity(value))
        {
            return "double.PositiveInfinity";
        }

        if (Double.IsNegativeInfinity(value))
        {
            return "double.NegativeInfinity";
        }

        return value.ToString("R", CultureInfo.InvariantCulture) + "d";
    }
}
