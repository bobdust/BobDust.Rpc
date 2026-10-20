using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld.Dummies;

namespace BobDust.Rpc.Sockets.Tests;

public class Template
{
    private const int DEFAULT_PORT = 1234;
    public static async Task Main(string[] args, Func<int, Settings?, object[], Task> main, params object[] inputs)
    {
        if (args.Length == 0)
        {
            await main(DEFAULT_PORT, default, inputs);
        }
        await Main(args, 0, default, main, inputs);
        await Main(args, 1, Settings.Json, main, inputs);
        await Main(args, 2, Settings.DataContract([typeof(GreetingOptions), typeof(GreetingResult)]), main, inputs);
        await Main(args, 3, Settings.Xml, main, inputs);
    }

    private static async Task Main(string[] args, int index, Settings? settings, Func<int, Settings?, object[], Task> main, object[] inputs)
    {
        if (args.Length > index && int.TryParse(args[index], out int port))
        {
            await main(port, settings, inputs);
        }
    }
}
