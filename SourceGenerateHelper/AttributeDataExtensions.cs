namespace SourceGenerateHelper;

using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

public static class AttributeDataExtensions
{
    public static bool TryGetConstructorArgument(this AttributeData attribute, int index, out TypedConstant value)
    {
        var arguments = attribute.ConstructorArguments;
        if ((index >= 0) && (index < arguments.Length) && (arguments[index].Kind != TypedConstantKind.Error))
        {
            value = arguments[index];
            return true;
        }

        value = default;
        return false;
    }

    public static bool TryGetConstructorArgument<T>(this AttributeData attribute, int index, [MaybeNullWhen(false)] out T value)
    {
        if (attribute.TryGetConstructorArgument(index, out var constant))
        {
            return constant.TryGetValue(out value);
        }

        value = default;
        return false;
    }

    public static bool TryGetNamedArgument(this AttributeData attribute, string name, out TypedConstant value)
    {
        var found = false;
        value = default;
        foreach (var argument in attribute.NamedArguments)
        {
            if (String.Equals(argument.Key, name, StringComparison.Ordinal))
            {
                value = argument.Value;
                found = true;
            }
        }

        return found && (value.Kind != TypedConstantKind.Error);
    }

    public static bool TryGetNamedArgument<T>(this AttributeData attribute, string name, [MaybeNullWhen(false)] out T value)
    {
        if (attribute.TryGetNamedArgument(name, out var constant))
        {
            return constant.TryGetValue(out value);
        }

        value = default;
        return false;
    }
}
