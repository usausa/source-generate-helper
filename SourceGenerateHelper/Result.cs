namespace SourceGenerateHelper;

using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

public sealed record Result<TValue>(TValue Value, EquatableArray<DiagnosticInfo> Diagnostics)
    where TValue : IEquatable<TValue>
{
    public bool HasDiagnostics => Diagnostics.Count > 0;

    public bool HasErrors
    {
        get
        {
            for (var i = 0; i < Diagnostics.Count; i++)
            {
                if (Diagnostics[i].Descriptor.DefaultSeverity == DiagnosticSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [MemberNotNullWhen(true, nameof(Value))]
    public bool HasValue { get; init; } = Value is not null;
}

public static class Results
{
    public static Result<TValue> Success<TValue>(TValue value)
        where TValue : IEquatable<TValue>
        => new(value, EquatableArray<DiagnosticInfo>.Empty);

    public static Result<TValue> Error<TValue>(DiagnosticInfo diagnostic)
        where TValue : IEquatable<TValue>
        => new(default!, new EquatableArray<DiagnosticInfo>([diagnostic])) { HasValue = false };

    public static Result<TValue> Errors<TValue>(params DiagnosticInfo[] diagnostics)
        where TValue : IEquatable<TValue>
        => new(default!, diagnostics) { HasValue = false };
}
