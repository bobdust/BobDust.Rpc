using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld;

const string ip = "127.0.0.1";
const string host = "localhost";
const int port = 1234;
var assistant = ClientFactory
    //.WithSettings(Settings.DataContract([typeof(GreetingOptions)]))
    .WithSettings(Settings.Json)
    //.WithSettings(Settings.Xml)
    //.Default
    .Get<IGreetingAssistant>(host, port);
Console.Write("Your name: ");
var name = Console.ReadLine();
var words = assistant.Hello(new GreetingOptions { Name = name }, "Nice day");
Console.WriteLine(words);
Console.Write("Nickname: ");
var nickname = Console.ReadLine();
words = await assistant.HelloAsync(new GreetingOptions { Name = nickname }, "Good day");
Console.WriteLine(words);
await assistant.HelloAndForgetAsync(new GreetingOptions { Name = string.Empty }, "gday");
Console.WriteLine("HelloAndForgetAsync sent");

var gratitude = (await ClientFactory.ConnectAsync(ip, port)).Mount<IGratitude>().As<IGratitude>();
words = gratitude.Thanks(new GreetingOptions { Name = name }, "Have a good day");
Console.WriteLine(words);
gratitude.ThanksAway(new GreetingOptions { Name = string.Empty }, "Have a good day");
Console.WriteLine("ThanksAway sent");
words = await gratitude.ThanksAsync(new GreetingOptions { Name = nickname }, "Have a good day");
Console.WriteLine(words);
await gratitude.ThanksAwayAsync(new GreetingOptions { Name = string.Empty }, "Have a good day");
Console.WriteLine("ThanksAwayAsync sent");

Console.ReadLine();
