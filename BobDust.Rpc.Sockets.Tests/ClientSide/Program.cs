using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld.Dummies;

namespace BobDust.Rpc.Sockets.Tests.ClientSide;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.Write("Your name: ");
        var name = Console.ReadLine();
        Console.Write("Nickname: ");
        var nickname = Console.ReadLine();
        await Template.Main(args, Main, name, nickname);
        Console.ReadLine();
    }

    private static async Task Main(int port, Settings? settings, object[] inputs)
    {
        const string ip = "127.0.0.1";
        const string host = "localhost";
        ClientFactory clientFactory;
        //const int port = 1234;
        var assistant = (clientFactory = settings is null ? ClientFactory.Default : ClientFactory.WithSettings(settings))
            //.WithSettings(Settings.DataContract([typeof(GreetingOptions), typeof(GreetingResult)]))
            //.WithSettings(Settings.Json)
            //.WithSettings(Settings.Xml)
            //.Default
            .Get<IGreetingAssistant>(host, port);
        Console.WriteLine($"---Begin {clientFactory.Settings.DataFormat}---");
        string name = (string)inputs[0];
        string nickname = (string)inputs[1];
        var words = assistant.Hello(new GreetingOptions { Name = name ?? string.Empty }, "Nice day");
        Console.WriteLine(words?.Result);
        words = await assistant.HelloAsync(new GreetingOptions { Name = nickname ?? string.Empty }, "Good day");
        Console.WriteLine(words?.Result);
        await assistant.HelloAndForgetAsync(new GreetingOptions { Name = string.Empty }, "gday");
        Console.WriteLine("HelloAndForgetAsync sent");

        var gratitude = (await ClientFactory.ConnectAsync(ip, port)).Mount<IGratitude>().As<IGratitude>();
        words = gratitude.Thanks(null, null);
        Console.WriteLine(words);
        gratitude.ThanksAway(new GreetingOptions { Name = string.Empty }, "Have a good day");
        Console.WriteLine("ThanksAway sent");
        words = await gratitude.ThanksAsync(null, null);
        Console.WriteLine(words);
        await gratitude.ThanksAwayAsync(new GreetingOptions { Name = string.Empty }, "Have a good day");
        Console.WriteLine("ThanksAwayAsync sent");

        try
        {
            assistant.Error();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        try
        {
            await assistant.ErrorAsync();
        }
        catch (Exception asyncEx)
        {
            Console.WriteLine(asyncEx);
        }
        Console.WriteLine($"---End {clientFactory.Settings.DataFormat}---");
    }
}
