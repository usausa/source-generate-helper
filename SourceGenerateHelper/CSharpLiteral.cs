namespace SourceGenerateHelper;

using System.Globalization;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class CSharpLiteral
{
    public static string Format(string value) =>
        SymbolDisplay.FormatLiteral(value, quote: true);

    public static string Format(char value) =>
        SymbolDisplay.FormatLiteral(value, quote: true);

    public static string? Format(object? value) => value switch
    {
        null => "null",
        string s => Format(s),
        char c => Format(c),
        bool b => b ? "true" : "false",
        float f => FormatSingle(f),
        double d => FormatDouble(d),
        decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
        long l => l.ToString(CultureInfo.InvariantCulture) + "L",
        ulong ul => ul.ToString(CultureInfo.InvariantCulture) + "uL",
        uint ui => ui.ToString(CultureInfo.InvariantCulture) + "u",
        int i => i.ToString(CultureInfo.InvariantCulture),
        short or ushort or byte or sbyte => "(" + GetKeyword(value) + ")" + ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture),
        _ => null
    };

    public static string? FormatEnum(ITypeSymbol enumType, object? value)
    {
        if ((enumType is not INamedTypeSymbol { TypeKind: TypeKind.Enum } type) || !type.CanBeNamedInOtherFile() ||
            (value is not (sbyte or byte or short or ushort or int or uint or long or ulong)))
        {
            return null;
        }

        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        foreach (var member in type.GetMembers())
        {
            if ((member is IFieldSymbol { HasConstantValue: true } field) && Equals(field.ConstantValue, value) && !field.IsObsolete())
            {
                return typeName + "." + CSharpIdentifier.Escape(field.Name);
            }
        }

        return "(" + typeName + ")(" + FormatUntyped(value) + ")";
    }

    internal static string? FormatUntyped(object? value) =>
        value is short or ushort or byte or sbyte ? ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture) : Format(value);

    private static string GetKeyword(object value) => value switch
    {
        short => "short",
        ushort => "ushort",
        byte => "byte",
        _ => "sbyte"
    };

    internal static string FormatSingle(float value)
    {
        if (Single.IsNaN(value))
        {
            return "float.NaN";
        }

        if (Single.IsInfinity(value))
        {
            return value > 0 ? "float.PositiveInfinity" : "float.NegativeInfinity";
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (!Single.Parse(text, CultureInfo.InvariantCulture).Equals(value))
        {
            text = value.ToString("G9", CultureInfo.InvariantCulture);
        }

        return WithNegativeZeroSign(text, value) + "f";
    }

    internal static string FormatDouble(double value)
    {
        if (Double.IsNaN(value))
        {
            return "double.NaN";
        }

        if (Double.IsInfinity(value))
        {
            return value > 0 ? "double.PositiveInfinity" : "double.NegativeInfinity";
        }

        var text = value.ToString("R", CultureInfo.InvariantCulture);
        if (!Double.Parse(text, CultureInfo.InvariantCulture).Equals(value))
        {
            text = value.ToString("G17", CultureInfo.InvariantCulture);
        }

        return WithNegativeZeroSign(text, value) + "d";
    }

    private static string WithNegativeZeroSign(string text, double value) =>
        (BitConverter.DoubleToInt64Bits(value) == Int64.MinValue) && (text[0] != '-') ? "-" + text : text;
}
