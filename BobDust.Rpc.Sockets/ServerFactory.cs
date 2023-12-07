using System;
using System.Linq;
using BobDust.Core.Extensions.Reflection.Emit;
using System.Collections.Concurrent;

namespace BobDust.Rpc.Sockets
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

		public Server<TExecutor> Get<TExecutor>(int port)
		{
			var executorType = typeof(TExecutor);
			var key = (port, executorType);
			var server = default(Server<TExecutor>);
			lock(_objects)
			{
				if (_objects.ContainsKey(key))
				{
					 server = (Server<TExecutor>)_objects[key];
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

		private Server<TExecutor> BuildServer<TExecutor>(int port, Type executorType)
		{
			Func<TExecutor> factory = () => (TExecutor)Activator.CreateInstance(executorType);
			var baseType = typeof(Server<>).MakeGenericType(executorType);
			var type = baseType.Extend(executorType.Name);
			return (Server<TExecutor>)type.GetConstructor(new[] { typeof(int), typeof(Func<TExecutor>) }).Invoke(new object[] { port, factory });
		}

	}
}
