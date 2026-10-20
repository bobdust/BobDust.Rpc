# BobDust.Rpc #

* Quick summary: RPC socket library.
* Version: 1.1.0.
* Dependencies:
  * .NET 10
  * .NET 8
  * .NET 4.8

## Usage ##

Reference to nuget package

```shell
dotnet add package BobDust.Rpc.Sockets --version 1.1.0
```

```xml
<ItemGroup>
  <PackageReference Include="BobDust.Rpc.Sockets" Version="1.1.0" />
</ItemGroup>
```

Define service contracts

```csharp
namespace BobDust.Rpc.Sockets.HelloWorld.Dummies;

public interface IGreetingAssistant
{
  GreetingResult Hello(GreetingOptions options, string greetingFollowup);
}

public interface IGratitude
{
  Task<GreetingResult?> ThanksAsync(GreetingOptions? options, string? greetingFollowup);
}

[Serializable]
public class GreetingOptions
{
  public string? Name { get; set; }
}

[Serializable]
public record GreetingResult
{
  public string? Result { get; set; }
}

```

Define service implementations

```csharp
using BobDust.Rpc.Sockets.HelloWorld.Dummies;

namespace BobDust.Rpc.Sockets.HelloWorld.Stub;

public class GreetingAssistant : IGreetingAssistant
{
  public GreetingResult Hello(GreetingOptions options, string greetingFollowup)
  {
    return new GreetingResult { Result = $"Hello, {options?.Name}. {greetingFollowup}!" };
  }
}

public class Gratitude : IGratitude
{
  public Task<GreetingResult?> ThanksAsync(GreetingOptions? options, string? gratitudeFollowup)
  {
    if (options is null || gratitudeFollowup is null)
    {
      return default;
    }

    IEnumerable<string> ThanksIterate()
    {
      yield return "Thanks";
      yield return $" {options?.Name ?? "world"}";
      yield return ". ";
      yield return gratitudeFollowup;
    }
    var builder = new System.Text.StringBuilder();
    foreach (var part in ThanksIterate())
    {
      builder.Append(part);
    }
    return Task.FromResult(new GreetingResult{ Result = builder.ToString()});
  }
}
```

Run services at server-side

```csharp
using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld.Dummies;
using BobDust.Rpc.Sockets.HelloWorld.Stub;

const int port = 1234;
ServerFactory
  .Listen(port)
  .Register<IGreetingAssistant, GreetingAssistant>(() => new GreetingAssistant())
  .Register<IGratitude, Gratitude>(() => new Gratitude())
  .Start();
```

Call services at client-side

```csharp
using BobDust.Rpc.Sockets.Builders;
using BobDust.Rpc.Sockets.HelloWorld.Dummies;

const string host = "localhost";
const int port = 1234;
var factory = await ClientFactory.ConnectAsync(host, port);
var channel = factory
  .Mount<IGreetingAssistant>()
  .Mount<IGratitude>();

var assistant = channel.As<IGreetingAssistant>();
var words = assistant.Hello(new GreetingOptions { Name = "world" }, "Nice day");
Console.WriteLine(words?.Result);

var gratitude = channel.As<IGratitude>();
words = await gratitude.ThanksAsync(new GreetingOptions { Name = "kitty" }, "Have a good day");
Console.WriteLine(words);
```

## License ##

This project is licensed under the Commercial Restricted License (CRL) v1.1.
See [LICENSE.md](LICENSE.md) for details.

For commercial use, please contact [ceillingbob@gmail.com] for licensing options.

> OOP is the bone of my framework
>
> Principle is my body and pattern is my blood
>
> I have created over a thousand codes
>
> Unknown to Book, nor known to Library
>
> Have a lot of pain troubleshooting issues
>
> Yet, those hands will never write anything
>
> So as I pray,
>
> ***unlimited open source***.
