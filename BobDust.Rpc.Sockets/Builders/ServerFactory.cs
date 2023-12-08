using System;
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

		private ConcurrentDictionary<(int Port, Type ExecutorType), object> _objects;

		private ServerFactory()
		{
			_objects = new ConcurrentDictionary<(int Port, Type ExecutorType), object>();
		}

		public IServer<TExecutor> Get<TExecutor>(int port) where TExecutor : class
		{
			var executorType = typeof(TExecutor);
			var key = (port, executorType);
			var server = default(IServer<TExecutor>);
			lock(_objects)
			{
				if (_objects.ContainsKey(key))
				{
					 server = (IServer<TExecutor>)_objects[key];
				}
				else
				{
					server = BuildServer<TExecutor>(port, executorType);
					_objects[key] = server;
				}
			}
			if (server != default)
			{
				return server;
			}
			throw new NotSupportedException();
		}

		private IServer<TExecutor> BuildServer<TExecutor>(int port, Type executorType) where TExecutor : class
		{
			Func<TExecutor> factory = () => (TExecutor)Activator.CreateInstance(executorType);
			var baseType = typeof(Server<>).MakeGenericType(executorType);
			var type = baseType.Extend(executorType.Name);
			Func<byte[], ICommand> commandFactory = BuildCommand;
			return (IServer<TExecutor>)type
				.GetConstructor(new[] { typeof(int), typeof(Func<TExecutor>), typeof(Func<byte[], ICommand>) })
				.Invoke(new object[] { port, factory, commandFactory });
		}

		private ICommand BuildCommand(byte[] bytes)
		{
			return BinarySequence.FromBytes<BinaryCommand>(bytes);
		}
	}
}
