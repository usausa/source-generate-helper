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

            if (String.IsNullOrEmpty(value))
            {
                return default!;
            }

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (targetType == typeof(int))
            {
                var result = int.Parse(value, CultureInfo.InvariantCulture);
                return Unsafe.As<int, T>(ref result);
            }
            if (targetType == typeof(bool))
            {
                var result = bool.Parse(value);
                return Unsafe.As<bool, T>(ref result);
            }
            if (targetType == typeof(long))
            {
                var result = long.Parse(value, CultureInfo.InvariantCulture);
                return Unsafe.As<long, T>(ref result);
            }
            if (targetType == typeof(double))
            {
                var result = double.Parse(value, CultureInfo.InvariantCulture);
                return Unsafe.As<double, T>(ref result);
            }
            if (targetType == typeof(float))
            {
                var result = float.Parse(value, CultureInfo.InvariantCulture);
                return Unsafe.As<float, T>(ref result);
            }
            if (targetType == typeof(decimal))
            {
                var result = decimal.Parse(value, CultureInfo.InvariantCulture);
                return Unsafe.As<decimal, T>(ref result);
            }

            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        return default!;
    }
}
