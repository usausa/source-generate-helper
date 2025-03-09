namespace SourceGenerateHelper;

using System.Globalization;
using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis.Diagnostics;

public static class AnalyzerConfigExtensions
{
    public static T GetValue<T>(this AnalyzerConfigOptions options, string key)
    {
        if (options.TryGetValue($"build_property.{key}", out var value))
        {
            if (typeof(T) == typeof(string))
            {
                return Unsafe.As<string, T>(ref value);
            }

            return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), CultureInfo.InvariantCulture);
        }

        return default!;
    }
}
