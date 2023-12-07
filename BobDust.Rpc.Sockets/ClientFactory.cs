using System;
using System.Reflection;
using BobDust.Core.Extensions.Reflection.Emit;
using System.Collections.Concurrent;

namespace BobDust.Rpc.Sockets
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
			var constructor = type.GetConstructor(new[] { typeof(string), typeof(int) });
			try
			{
				instance = (T)constructor.Invoke(new object[] { host, port });
				_objects[key] = instance;
				return instance;
			}
			catch (Exception ex)
			{
				throw (ex.InnerException);
			}
		}

	}

}
