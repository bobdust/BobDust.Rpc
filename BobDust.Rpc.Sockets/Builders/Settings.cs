using System.Dynamic;
using BobDust.Core.Extensions.Reflection;

namespace BobDust.Rpc.Sockets.Builders;

public record Settings
{
    public static Settings Default { get; } = new Settings(DataFormatType.Binary);

    public static Settings DataContract(IEnumerable<Type> knownTypes) => new Settings(DataFormatType.DataContract, new { KnownTypes = knownTypes });

    public static Settings Json { get; } = new Settings(DataFormatType.Json);

    private Settings(DataFormatType dataFormat)
    {
        DataFormat = dataFormat;
    }

    private Settings(DataFormatType dataFormat, object extra)
    {
        DataFormat = dataFormat;
        Extra = extra.ToExpando();
    }

    public DataFormatType DataFormat { get; }
    public ExpandoObject? Extra { get; }
}
