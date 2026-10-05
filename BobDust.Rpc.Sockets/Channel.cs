using System.Net.Sockets;
using System.Net;
using BobDust.Core.ExceptionHandling;
using BobDust.Rpc.Sockets.Abstractions;
using System.Collections.Concurrent;

namespace BobDust.Rpc.Sockets
{
	class Channel : ExceptionHandler, IChannel
	{
		private readonly string _host;
		private readonly int _port;
		private readonly Func<string, string, IEnumerable<(string, object)>, ICommand> _commandFactory;
		private readonly Func<byte[], ICommandResult> _commandResultFactory;
		private readonly Func<Type, Func<ICommand, ICommandResult>, Func<ICommand, Task<ICommandResult>>, object> _clientFactory;
		private TcpClient? _client;
		private CommandPipeline? _pipeline;
		public bool IsDisposed { get; private set; }
		private ConcurrentDictionary<Type, object> _clients;

		public Channel(
			string host,
			int port,
			Func<string, string, IEnumerable<(string, object)>, ICommand> commandFactory,
			Func<byte[], ICommandResult> commandResultFactory,
			Func<Type, Func<ICommand, ICommandResult>, Func<ICommand, Task<ICommandResult>>, object> clientFactory
		)
		{
			_host = host;
			_port = port;
			_commandFactory = commandFactory;
			_commandResultFactory = commandResultFactory;
			_clientFactory = clientFactory;
			_clients = new ConcurrentDictionary<Type, object>();
		}

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
				var serverEndpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
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
			return this;
		}

		public async Task<IChannel> ConnectAsync()
		{
			if (_client is null || IsDisposed)
			{
				var serverEndpoint = new IPEndPoint(IPAddress.Parse(_host), _port);
				_client = new TcpClient(serverEndpoint.AddressFamily);
				await _client.ConnectAsync(serverEndpoint);
				var sendSocket = _client.Client;
				var receiveSocket = new Socket(sendSocket.AddressFamily, sendSocket.SocketType, sendSocket.ProtocolType);
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
