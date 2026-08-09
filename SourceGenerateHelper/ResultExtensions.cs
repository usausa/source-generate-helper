namespace SourceGenerateHelper;

using System;
using System.Collections.Generic;

public static class ResultExtensions
{
    // Whether a value is usable is the builder's call: Results.Error supplies none, and a
    // Result built with one is emitted even when diagnostics are attached to it.
    public static IEnumerable<TValue> SelectValue<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : IEquatable<TValue> =>
        source.Where(static x => x.HasValue).Select(static x => x.Value);

    public static IEnumerable<DiagnosticInfo> SelectError<TValue>(this IEnumerable<Result<TValue>> source)
        where TValue : IEquatable<TValue> =>
        source.SelectMany(static x => x.Diagnostics);
}
