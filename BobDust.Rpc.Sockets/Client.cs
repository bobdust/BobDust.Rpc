using System.Net.Sockets;
using System.Net;
using BobDust.Core.ExceptionHandling;
using BobDust.Rpc.Sockets.Abstractions;
using System.Runtime.CompilerServices;

namespace BobDust.Rpc.Sockets
{
	public abstract class Client : ExceptionHandler, IDisposable
	{
		private readonly Func<string, IEnumerable<(string, object)>, ICommand> _commandFactory;
		private readonly Func<byte[], ICommandResult> _commandResultFactory;
		private readonly TcpClient _client;
		private readonly CommandPipeline _pipeline;
		public bool IsDisposed { get; private set; }

		protected Client(
			string host, 
			int port, 
			Func<string, IEnumerable<(string, object)>, ICommand> commandFactory, 
			Func<byte[], ICommandResult> commandResultFactory
		)
		{
			_commandFactory = commandFactory;
			_commandResultFactory = commandResultFactory;

			var serverEndpoint = new IPEndPoint(IPAddress.Parse(host), port);
			_client = new TcpClient(serverEndpoint.AddressFamily);
			_client.Connect(serverEndpoint);
			var sendSocket = _client.Client;
			var receiveSocket = new Socket(sendSocket.AddressFamily, sendSocket.SocketType, sendSocket.ProtocolType);
			receiveSocket.Connect(sendSocket.RemoteEndPoint!);
            _pipeline = new CommandPipeline(new SocketPipeline(sendSocket, receiveSocket), Deserialize)
            {
                OnException = (ex, source) =>
                {
                    Dispose();
                }
            };
            _pipeline.Open();
		}

		protected ICommandResult Send(ICommand command)
		{
			var result = _pipeline.Post(command);
			return (ICommandResult)result;
		}

		protected async Task<ICommandResult> SendAsync(ICommand command)
		{
			var result = await _pipeline.PostAsync(command);
			return (ICommandResult)result;
		}

		protected object? Invoke(object[] values, [CallerMemberName] string methodName = "", string[]? parameters = null)
		{
			var command = _commandFactory(methodName, (parameters ?? Array.Empty<string>()).Select((name, i) => (name, values[i])).ToArray());
			var result = Send(command);
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

		private async Task<object?> InvokeAsyncInternal(string methodName, string[] parameters, object[] values)
		{
			var command = _commandFactory(methodName, parameters.Select((name, i) => (name, values[i])).ToArray());
			var result = await SendAsync(command);
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

		protected async Task<T?> InvokeAsync<T>(object[] values, [CallerMemberName] string caller = "", string[]? parameterNames = null)
		{
			var result = await InvokeAsyncInternal(caller, parameterNames ?? Array.Empty<string>(), values);
			return result is T typedResult ? typedResult : default;
		}

		protected ICommandResult Deserialize(byte[] bytes)
		{
			return _commandResultFactory(bytes);
		}

		public void Dispose()
		{
			_pipeline.Dispose();
			_client.Close();
			IsDisposed = true;
		}
	}
}
