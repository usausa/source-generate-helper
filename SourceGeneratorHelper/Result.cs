namespace SourceGeneratorHelper;

public sealed record Result<TValue>(TValue Value, EquatableArray<DiagnosticInfo> Errors)
    where TValue : IEquatable<TValue>?
{
}
