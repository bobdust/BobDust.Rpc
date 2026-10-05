using BobDust.Core.Extensions.Reflection.Emit;
using System.Collections.Concurrent;
using BobDust.Rpc.Sockets.Abstractions;
using BobDust.Rpc.Sockets.Serialization;

namespace BobDust.Rpc.Sockets.Builders
{
	public class ServerFactory
	{
		private static readonly ServerFactory _instance = new ServerFactory();

		public static ServerFactory Default { get { return _instance; } }

		private ConcurrentDictionary<int, object> _objects;

		private ServerFactory()
		{
			_objects = new ConcurrentDictionary<int, object>();
		}

		private IServer Get(int port, Func<int, IServer> buildServer)
		{
			var key = port;
			var server = default(IServer);
			lock(_objects)
			{
				if (_objects.ContainsKey(key))
				{
					 server = (IServer)_objects[key];
				}
				else
				{
					server = buildServer(port);
					if (server == default)
					{
						throw new NotSupportedException($"Server for port {port} is not supported.");
					}
					_objects[key] = server;
				}
			}
			if (server != default)
			{
				return server;
			}
			throw new NotSupportedException();
		}

		public IServer<TExecutor> Get<TExecutor>(int port) where TExecutor : class
		{
			return (IServer<TExecutor>)Get(port, (p) => BuildServer<TExecutor>(p, typeof(TExecutor)) ?? throw new NotSupportedException($"Server for executor type {typeof(TExecutor).Name} is not supported."));
		}

		public static IServer Listen(int port)
		{
			return _instance.Get(port, _instance.BuildServer);
		}

		private IServer BuildServer(int port)
		{
			var baseType = typeof(Server);
			var type = baseType.Extend($"{nameof(Server)}{port}");
			Func<byte[], ICommand> commandFactory = BuildCommand;
			return (IServer?)type
				.GetConstructor([typeof(int), typeof(Func<byte[], ICommand>)])?
				.Invoke([port, commandFactory]) ?? throw new NotSupportedException($"Server for port {port} is not supported.");
		}

		private IServer<TExecutor>? BuildServer<TExecutor>(int port, Type executorType) where TExecutor : class
		{
			Func<TExecutor?> factory = () => (TExecutor?)Activator.CreateInstance(executorType);
			var baseType = typeof(Server<>).MakeGenericType(executorType);
			var type = baseType.Extend(executorType.Name);
			Func<byte[], ICommand> commandFactory = BuildCommand;
			return (IServer<TExecutor>?)type
				.GetConstructor([typeof(int), typeof(Func<TExecutor>), typeof(Func<byte[], ICommand>)])?
				.Invoke([port, factory, commandFactory]);
		}

		private ICommand BuildCommand(byte[] bytes)
		{
			return BinarySequence.FromBytes<BinaryCommand>(bytes);
		}
	}
}
