using BobDust.Core.ExceptionHandling;
using BobDust.Rpc.Sockets.Abstractions;
using System.Runtime.CompilerServices;

namespace BobDust.Rpc.Sockets
{
	public abstract class Client : ExceptionHandler
	{
		private readonly Func<string, string, IEnumerable<(string, object?, string)>, ICommand> _commandFactory;
		private readonly Func<ICommand, ICommandResult> _send;
		private readonly Func<ICommand, Task<ICommandResult>> _sendAsync;

		protected Client(
			Func<string, string, IEnumerable<(string, object?, string)>, ICommand> commandFactory, 
			Func<ICommand, ICommandResult> send,
			Func<ICommand, Task<ICommandResult>> sendAsync
		)
		{
			_commandFactory = commandFactory;
			_send = send;
			_sendAsync = sendAsync;
		}

		protected object? Invoke(object?[] values, string contractType, [CallerMemberName] string methodName = "", string[]? parameterNames = null, string[]? parameterTypes = null)
		{
			var command = _commandFactory(contractType, methodName, (parameterNames ?? Array.Empty<string>()).Select((name, i) => (name, values[i], parameterTypes![i])).ToArray());
			var result = _send(command);
			if (result.ReturnValue != null)
			{
				return result.ReturnValue;
			}
			else if (result.Exception != null)
			{
				Handle(result.Exception, this);
			}
			return null;
		}

		private async Task<object?> InvokeAsyncInternal(string contractType, string methodName, (string Name, string Type)[] parameters, object?[] values)
		{
			var command = _commandFactory(contractType, methodName, parameters.Select((t, i) => (t.Name, values[i], t.Type)).ToArray());
			var result = await _sendAsync(command);
			if (result.ReturnValue != null)
			{
				return result.ReturnValue;
			}
			else if (result.Exception != null)
			{
				Handle(result.Exception, this);
			}
			return null;
		}

		protected async Task<T?> InvokeAsync<T>(object[] values, string contractType, [CallerMemberName] string caller = "", string[]? parameterNames = null, string[]? parameterTypes = null)
		{
			var result = await InvokeAsyncInternal(contractType, caller, (parameterNames ?? Array.Empty<string>()).Select((name, i) => (name, parameterTypes![i])).ToArray(), values);
			return result is T typedResult ? typedResult : default;
		}
    }
}
