using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld.Dummies;

const string ip = "127.0.0.1";
const string host = "localhost";
const int port = 1234;
var assistant = ClientFactory
    //.WithSettings(Settings.DataContract([typeof(GreetingOptions), typeof(GreetingResult)]))
    //.WithSettings(Settings.Json)
    //.WithSettings(Settings.Xml)
    .Default
    .Get<IGreetingAssistant>(host, port);
Console.Write("Your name: ");
var name = Console.ReadLine();
var words = assistant.Hello(new GreetingOptions { Name = name ?? string.Empty }, "Nice day");
Console.WriteLine(words?.Result);
Console.Write("Nickname: ");
var nickname = Console.ReadLine();
words = await assistant.HelloAsync(new GreetingOptions { Name = nickname ?? string.Empty }, "Good day");
Console.WriteLine(words?.Result);
await assistant.HelloAndForgetAsync(new GreetingOptions { Name = string.Empty }, "gday");
Console.WriteLine("HelloAndForgetAsync sent");

var gratitude = (await ClientFactory.ConnectAsync(ip, port)).Mount<IGratitude>().As<IGratitude>();
words = gratitude.Thanks(new GreetingOptions { Name = name ?? string.Empty }, "Have a good day");
Console.WriteLine(words);
gratitude.ThanksAway(new GreetingOptions { Name = string.Empty }, "Have a good day");
Console.WriteLine("ThanksAway sent");
words = await gratitude.ThanksAsync(new GreetingOptions { Name = nickname ?? string.Empty }, "Have a good day");
Console.WriteLine(words);
await gratitude.ThanksAwayAsync(new GreetingOptions { Name = string.Empty }, "Have a good day");
Console.WriteLine("ThanksAwayAsync sent");

try
{
    assistant.Error();
}
catch(Exception ex)
{
    Console.WriteLine(ex.Message);
}
try
{
    await assistant.ErrorAsync();
}
catch(Exception asyncEx)
{
    Console.WriteLine(asyncEx.Message);
}

Console.ReadLine();
