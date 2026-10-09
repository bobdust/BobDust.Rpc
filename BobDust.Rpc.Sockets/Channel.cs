using System.Net.Sockets;
using System.Net;
using BobDust.Core.ExceptionHandling;
using BobDust.Rpc.Sockets.Abstractions;
using System.Collections.Concurrent;

namespace BobDust.Rpc.Sockets
{
	class Channel(
		string host,
		int port,
		Func<string, string, IEnumerable<(string, object)>, ICommand> commandFactory,
		Func<byte[], ICommandResult> commandResultFactory,
		Func<Type, Func<ICommand, ICommandResult>, Func<ICommand, Task<ICommandResult>>, object> clientFactory
		) : ExceptionHandler, IChannel
	{
		private readonly string _host = host;
		private readonly int _port = port;
		private readonly Func<string, string, IEnumerable<(string, object)>, ICommand> _commandFactory = commandFactory;
		private readonly Func<byte[], ICommandResult> _commandResultFactory = commandResultFactory;
		private readonly Func<Type, Func<ICommand, ICommandResult>, Func<ICommand, Task<ICommandResult>>, object> _clientFactory = clientFactory;
		private TcpClient? _client;
		private CommandPipeline? _pipeline;
		public bool IsDisposed { get; private set; }
		private ConcurrentDictionary<Type, object> _clients = new ConcurrentDictionary<Type, object>();

		protected ICommandResult Send(ICommand command)
		{
			var result = _pipeline!.Post(command);
			return (ICommandResult)result;
		}

		protected async Task<ICommandResult> SendAsync(ICommand command)
		{
			var result = await _pipeline!.PostAsync(command);
			return (ICommandResult)result;
		}

		protected ICommandResult Deserialize(byte[] bytes)
		{
			return _commandResultFactory(bytes);
		}

		public void Dispose()
		{
			_pipeline?.Dispose();
			_client?.Close();
			IsDisposed = true;
		}

		public IChannel Mount<T>()
		{
			var contractType = typeof(T);
			if (!_clients.ContainsKey(contractType))
			{
				var client = _clientFactory(contractType, Send, SendAsync);
				_clients[contractType] = client;
			}

			return this;
		}

		public T As<T>()
		{
			if (_clients.TryGetValue(typeof(T), out var client))
			{
				return (T)client;
			}
			throw new InvalidOperationException($"Client for type {typeof(T)} is not mounted.");
		}

		public IChannel Connect()
		{
			if (_client is null || IsDisposed)
			{
				_client = new TcpClient();
				_client.Connect(_host, _port);
				var sendSocket = _client.Client;
				var receiveSocket = new Socket(sendSocket.AddressFamily, sendSocket.SocketType, sendSocket.ProtocolType)
				{
					DualMode = true
				};
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
			return this;
		}

		public async Task<IChannel> ConnectAsync()
		{
			if (_client is null || IsDisposed)
			{
				_client = new TcpClient();
				await _client.ConnectAsync(_host, _port);
				var sendSocket = _client.Client;
				var receiveSocket = new Socket(sendSocket.AddressFamily, sendSocket.SocketType, sendSocket.ProtocolType)
				{
					DualMode = true
				};
				await receiveSocket.ConnectAsync(sendSocket.RemoteEndPoint!);
				_pipeline = new CommandPipeline(new SocketPipeline(sendSocket, receiveSocket), Deserialize)
				{
					OnException = (ex, source) =>
					{
						Dispose();
					}
				};
				_pipeline.Open();
			}
			return this;
		}
	}
}
