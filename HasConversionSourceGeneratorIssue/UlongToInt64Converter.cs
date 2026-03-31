using BLite.Core.Metadata;

namespace HasConversionSourceGeneratorIssue;

public class UlongToInt64Converter : ValueConverter<ulong, long>
{
    public override long ConvertToProvider(ulong value)
    {
        return (long)value;
    }

    public override ulong ConvertFromProvider(long provider)
    {
        return (ulong)provider;
    }
}