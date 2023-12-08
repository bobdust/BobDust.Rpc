using System;
using System.Reflection;
using BobDust.Core.Extensions.Reflection.Emit;
using System.Collections.Concurrent;
using BobDust.Rpc.Sockets.Serialization;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets.Builders
{
	public class ClientFactory
	{
		private static readonly ClientFactory _instance = new ClientFactory();

		public static ClientFactory Default { get { return _instance; } }

		private ConcurrentDictionary<string, object> _objects;

		private ClientFactory()
		{
			_objects = new ConcurrentDictionary<string, object>();
		}

		public T Get<T>(string host, int port)
		{
			var key = string.Format("{0}", typeof(T).FullName);
			T instance = default(T);
			lock (_objects)
			{
				if (_objects.ContainsKey(key))
				{
					var client = _objects[key] as Client;
					if (client.IsDisposed)
					{
						object value;
						_objects.TryRemove(key, out value);
					}
					else
					{
						instance = (T)_objects[key];
					}
				}
			}
			if (instance != null)
			{
				return instance;
			}

			var contractType = typeof(T);
			var baseType = typeof(Client);
			var invoke = baseType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic);
			var type = baseType.Implement(contractType, (method) =>
			{
				return invoke;
			});
			var constructor = type.GetConstructor(new[] { typeof(string), typeof(int), typeof(Func<string, ICommand>), typeof(Func<byte[], ICommandResult>) });
			try
			{
				Func<string, ICommand> commandFactory = BuildCommand;
				Func<byte[], ICommandResult> commandResultFactory = BuildCommandResult;
				instance = (T)constructor.Invoke(new object[] { host, port, commandFactory, commandResultFactory });
				_objects[key] = instance;
				return instance;
			}
			catch (Exception ex)
			{
				throw (ex.InnerException);
			}
		}

		private ICommandResult BuildCommandResult(byte[] bytes)
		{
			return BinarySequence.FromBytes<BinaryCommandResult>(bytes);
		}

		private ICommand BuildCommand(string method)
		{
			return new BinaryCommand(method) as ICommand;
		}
	}

}
