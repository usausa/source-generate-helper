namespace SourceGenerateHelper;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

public record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
    public Location ToLocation() =>
        Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? CreateFrom(SyntaxNode node) =>
        CreateFrom(node.GetLocation());

    public static LocationInfo? CreateFrom(Location location) =>
        location.SourceTree is null
            ? null
            : new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
}

#pragma warning disable CA1819
public sealed record DiagnosticInfo
{
    public DiagnosticDescriptor Descriptor { get; }

    public LocationInfo? Location { get; }

    public ImmutableDictionary<string, string?>? Properties { get; }

    public EquatableArray<string> MessageArgs { get; }

    public DiagnosticInfo(
        DiagnosticDescriptor descriptor,
        Location? location)
        : this(descriptor, location, (ImmutableDictionary<string, string?>?)null, null)
    {
    }

    public DiagnosticInfo(
        DiagnosticDescriptor descriptor,
        Location? location,
        ImmutableDictionary<string, string?>? properties)
        : this(descriptor, location, properties, null)
    {
    }

    public DiagnosticInfo(
        DiagnosticDescriptor descriptor,
        Location? location,
        params string[]? messageArgs)
        : this(descriptor, location, null, messageArgs)
    {
    }

    public DiagnosticInfo(
        DiagnosticDescriptor descriptor,
        LocationInfo? location,
        params string[]? messageArgs)
        : this(descriptor, location, null, messageArgs)
    {
    }

    public DiagnosticInfo(
        DiagnosticDescriptor descriptor,
        Location? location,
        ImmutableDictionary<string, string?>? properties,
        params string[]? messageArgs)
    {
        Descriptor = descriptor;
        Location = location is not null ? LocationInfo.CreateFrom(location) : null;
        Properties = properties;
        MessageArgs = messageArgs ?? EquatableArray<string>.Empty;
    }

    public DiagnosticInfo(
        DiagnosticDescriptor descriptor,
        LocationInfo? location,
        ImmutableDictionary<string, string?>? properties,
        params string[]? messageArgs)
    {
        Descriptor = descriptor;
        Location = location;
        Properties = properties;
        MessageArgs = messageArgs ?? EquatableArray<string>.Empty;
    }

    // ReSharper disable once CoVariantArrayConversion
    public Diagnostic ToDiagnostic() => Diagnostic.Create(Descriptor, Location?.ToLocation(), Properties, MessageArgs);
}
#pragma warning restore CA1819
