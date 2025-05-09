namespace SourceGenerateHelper;

using System;
using System.Collections.Generic;

public static class ResultExtensions
{
    public static IEnumerable<TValue> SelectValue<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : IEquatable<TValue> =>
        source.Select(static x => x.Value).Where(static x => x is not null);

    public static IEnumerable<DiagnosticInfo> SelectError<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : IEquatable<TValue> =>
        source.Select(static x => x.Error).OfType<DiagnosticInfo>();
}
