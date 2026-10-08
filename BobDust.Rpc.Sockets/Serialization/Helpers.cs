using BobDust.Rpc.Sockets.Abstractions;
using BobDust.Rpc.Sockets.Builders;

namespace BobDust.Rpc.Sockets.Serialization;

public static class Helpers
{
    private static void SetKnownTypes(Settings settings)
    {
        if (settings.DataFormat == DataFormatType.DataContract
        && settings.Extra is IDictionary<string, object> dict
        && dict.TryGetValue("KnownTypes", out object? value)
        && value is IEnumerable<Type> knownTypes)
        {
            DataContractCommandBase.KnownTypes = knownTypes;
        }
    }

    public static Func<byte[], ICommand> BuildCommandFromBytes(Settings settings)
    {
        SetKnownTypes(settings);
        return settings.DataFormat switch
        {
            DataFormatType.DataContract => BinarySequence.FromBytes<DataContractCommand>,
            DataFormatType.Binary => BinarySequence.FromBytes<BinaryCommand>,
            DataFormatType.Xml => BinarySequence.FromBytes<XmlCommand>,
            DataFormatType.Json => BinarySequence.FromBytes<JsonCommand>,
            _ => throw new NotSupportedException($"Data format {settings.DataFormat} is not supported.")
        };
    }

    public static Func<byte[], ICommandResult> BuildCommandResultFromBytes(Settings settings)
    {
        SetKnownTypes(settings);
        return settings.DataFormat switch
        {
            DataFormatType.DataContract => BinarySequence.FromBytes<DataContractCommandResult>,
            DataFormatType.Binary => BinarySequence.FromBytes<BinaryCommandResult>,
            DataFormatType.Xml => BinarySequence.FromBytes<XmlCommandResult>,
            DataFormatType.Json => BinarySequence.FromBytes<JsonCommandResult>,
            _ => throw new NotSupportedException($"Data format {settings.DataFormat} is not supported.")
        };
    }

    public static Func<string, string, IEnumerable<(string Name, object Value)>, ICommand> BuildCommandFromMethod(Settings settings)
    {
        SetKnownTypes(settings);
        return settings.DataFormat switch
        {
            DataFormatType.DataContract => (contractType, method, parameters) => new DataContractCommand(contractType, method, parameters),
            DataFormatType.Binary => (contractType, method, parameters) => new BinaryCommand(contractType, method, parameters),
            DataFormatType.Xml => (contractType, method, parameters) => new XmlCommand(contractType, method, parameters),
            DataFormatType.Json => (contractType, method, parameters) => new JsonCommand(contractType, method, parameters),
            _ => throw new NotSupportedException($"Data format {settings.DataFormat} is not supported.")
        };
    }
}
