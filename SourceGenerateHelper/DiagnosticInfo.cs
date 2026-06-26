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

    public bool Equals(DiagnosticInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return EqualityComparer<DiagnosticDescriptor>.Default.Equals(Descriptor, other.Descriptor) &&
               Equals(Location, other.Location) &&
               MessageArgs.Equals(other.MessageArgs) &&
               PropertiesEqual(Properties, other.Properties);
    }

    private static bool PropertiesEqual(ImmutableDictionary<string, string?>? a, ImmutableDictionary<string, string?>? b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if ((a is null) || (b is null) || (a.Count != b.Count))
        {
            return false;
        }

        foreach (var pair in a)
        {
            if (!b.TryGetValue(pair.Key, out var value) || !String.Equals(pair.Value, value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = 17;
        hash = (hash * 31) + EqualityComparer<DiagnosticDescriptor>.Default.GetHashCode(Descriptor);
        hash = (hash * 31) + (Location?.GetHashCode() ?? 0);
        hash = (hash * 31) + MessageArgs.GetHashCode();

        if (Properties is not null)
        {
            var entriesHash = 0;
            foreach (var pair in Properties)
            {
                var entryHash = 17;
                entryHash = (entryHash * 31) + StringComparer.Ordinal.GetHashCode(pair.Key);
                entryHash = (entryHash * 31) + (pair.Value is null ? 0 : StringComparer.Ordinal.GetHashCode(pair.Value));
                entriesHash ^= entryHash;
            }

            hash = (hash * 31) + entriesHash;
        }

        return hash;
    }
}
#pragma warning restore CA1819
