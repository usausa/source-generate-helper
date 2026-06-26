namespace SourceGenerateHelper;

using System.Globalization;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.Diagnostics;

public static class AnalyzerConfigExtensions
{
    public static T GetValue<T>(this AnalyzerConfigOptions options, string key)
    {
        if (!options.TryGetValue($"build_property.{key}", out var value))
        {
            return default!;
        }

        if (typeof(T) == typeof(string))
        {
            return Unsafe.As<string, T>(ref value);
        }

        if (String.IsNullOrEmpty(value))
        {
            return default!;
        }

        try
        {
            return ConvertValue<T>(value!);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new InvalidOperationException($"AnalyzerConfig value for key '{key}' could not be converted to type '{typeof(T)}'. Value: '{value}'.", ex);
        }
    }

    public static bool TryGetValue<T>(this AnalyzerConfigOptions options, string key, out T value)
    {
        if (!options.TryGetValue($"build_property.{key}", out var raw))
        {
            value = default!;
            return false;
        }

        if (typeof(T) == typeof(string))
        {
            value = Unsafe.As<string, T>(ref raw);
            return true;
        }

        if (String.IsNullOrEmpty(raw))
        {
            value = default!;
            return false;
        }

        try
        {
            value = ConvertValue<T>(raw!);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            value = default!;
            return false;
        }
    }

    private static T ConvertValue<T>(string value)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        var converted = targetType.IsEnum
            ? Enum.Parse(targetType, value, ignoreCase: true)
            : Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        return (T)converted;
    }
}
