namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class TypedConstantExtensions
{
    public static string ToCSharpStringWithPostfix(this TypedConstant constant)
    {
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
}
