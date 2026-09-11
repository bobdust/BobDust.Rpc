using System.Net.Sockets;
using System.Net;
using BobDust.Core.ExceptionHandling;
using System.Diagnostics;
using System.Text.RegularExpressions;
using BobDust.Rpc.Sockets.Abstractions;

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
			receiveSocket.Connect(sendSocket.RemoteEndPoint);
			_pipeline = new CommandPipeline(new SocketPipeline(sendSocket, receiveSocket), Deserialize);
			_pipeline.OnException = (ex, source) =>
			{
				Dispose();
			};
			_pipeline.Open();
		}

		protected ICommandResult Send(ICommand command)
		{
			var result = _pipeline.Post(command);
			return (ICommandResult)result;
		}

		protected object Invoke(params object[] values)
		{
			var stackTrace = new StackTrace();
			var methodInfo = stackTrace.GetFrame(1).GetMethod();
			const string pattern = @"((?<interface>\w+).)?(?<methodName>\w+)";
			var methodName = Regex.Match(methodInfo.Name, pattern).Groups["methodName"].Value;
			var parameters = methodInfo.GetParameters();
			var command = _commandFactory(methodName, parameters.Select((p, i) => (p.Name, values[i])).ToArray());
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
