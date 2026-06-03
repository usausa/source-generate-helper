namespace SourceGenerateHelper;

public sealed record Result<TValue>(TValue Value, EquatableArray<DiagnosticInfo> Diagnostics)
    where TValue : IEquatable<TValue>;

public static class Results
{
    public static Result<TValue> Success<TValue>(TValue value)
        where TValue : IEquatable<TValue>
        => new(value, EquatableArray<DiagnosticInfo>.Empty);

    public static Result<TValue> Error<TValue>(DiagnosticInfo diagnostic)
        where TValue : IEquatable<TValue>
        => new(default!, new EquatableArray<DiagnosticInfo>([diagnostic]));

    public static Result<TValue> Errors<TValue>(params DiagnosticInfo[] diagnostics)
        where TValue : IEquatable<TValue>
        => new(default!, diagnostics);
}
