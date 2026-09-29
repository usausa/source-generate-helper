namespace SourceGenerateHelper;

using Microsoft.CodeAnalysis;

public static class SymbolDisplayFormats
{
    public static SymbolDisplayFormat FullyQualifiedNullable { get; } =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
}
