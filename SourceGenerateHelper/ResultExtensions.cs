namespace SourceGenerateHelper;

using System;
using System.Collections.Generic;

public static class ResultExtensions
{
    public static IEnumerable<TValue> SelectValue<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : IEquatable<TValue> =>
        source.Where(static x => x.IsSuccess).Select(static x => x.Value);

    public static IEnumerable<DiagnosticInfo> SelectError<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : IEquatable<TValue> =>
        source.SelectMany(static x => x.Diagnostics);
}
