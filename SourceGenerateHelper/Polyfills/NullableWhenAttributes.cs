// ReSharper disable All
#pragma warning disable
#if !NET5_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class MaybeNullWhenAttribute : Attribute
{
    public MaybeNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    public bool ReturnValue { get; }
}

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
internal sealed class NotNullWhenAttribute : Attribute
{
    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    public bool ReturnValue { get; }
}
#endif
#pragma warning restore
