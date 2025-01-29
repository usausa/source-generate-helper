namespace SourceGenerateHelper;

using System;
using System.Collections.Generic;

internal static class ResultExtensions
{
    public static IEnumerable<TValue> SelectValue<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : IEquatable<TValue> =>
        source.Select(static x => x.Value);

    public static IEnumerable<DiagnosticInfo> SelectError<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : IEquatable<TValue> =>
        source.Select(static x => x.Error).OfType<DiagnosticInfo>();
}
