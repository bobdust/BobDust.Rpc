using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld;

const string host = "127.0.0.1";
const int port = 1234;
var assistant = ClientFactory.Default.Get<IGreetingAssistant>(host, port);
Console.Write("Your name: ");
var name = Console.ReadLine();
var words = assistant.Hello(new GreetingOptions { Name = name }, "Nice day");
Console.WriteLine(words);
Console.Write("Nickname: ");
var nickname = Console.ReadLine();
words = await assistant.HelloAsync(new GreetingOptions { Name = nickname }, "Good day");
Console.WriteLine(words);
await assistant.HelloAndForgetAsync(new GreetingOptions { }, "gday");
var tmp = "";
Console.ReadLine();
