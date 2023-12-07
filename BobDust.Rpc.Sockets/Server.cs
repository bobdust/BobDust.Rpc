using System;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using BobDust.Core.Threading;
using System.Collections.Concurrent;
using BobDust.Core.ExceptionHandling;
using System.Linq.Expressions;
using System.Reflection;

namespace BobDust.Rpc.Sockets
{
	public abstract class Server<TExecutor> : ExceptionHandler
	{
		private TcpListener _listener;
		private Runnable _listenTask;
		private ConcurrentBag<IPipeline> _pipelines;
		private AutoResetEvent _waitHandle;
		private bool _isStopped;
		private ConcurrentDictionary<string, TExecutor> _executors;
		private Func<TExecutor> _factory;

		protected Server(int port)
		{
			_listener = new TcpListener(IPAddress.Any, port);
			_listenTask = new Runnable(Listen);
			_pipelines = new ConcurrentBag<IPipeline>();
			_waitHandle = new AutoResetEvent(false);
			_executors = new ConcurrentDictionary<string, TExecutor>();
		}

		protected Server(int port, Func<TExecutor> factory)
		   : this(port)
		{
			_factory = factory;
		}

		public void Start()
		{
			_listener.Start();
			_listenTask.Start();
		}

		public void Stop()
		{
			_isStopped = true;
			foreach (var pipeline in _pipelines)
			{
				pipeline.Close();
			}
			_listenTask.Stop();
			_listener.Server.Close();
			_listener.Stop();
			_waitHandle.Set();
		}

		private void Listen()
		{
			_listener.BeginAcceptTcpClient(delegate (IAsyncResult asyncResult)
			{
				if (_isStopped)
				{
					return;
				}
				var client = _listener.EndAcceptTcpClient(asyncResult);
				var sendSocket = _listener.AcceptSocket();
				var receiveSocket = client.Client;
				var pipeline = new CommandPipeline(new SocketPipeline(sendSocket, receiveSocket), Deserialize);
				pipeline.OnReceived = Execute;
				pipeline.OnException = Handle;
				pipeline.Open();
				_pipelines.Add(pipeline);
				_waitHandle.Set();
			}, null);
			_waitHandle.WaitOne();
		}

		protected virtual TExecutor GetExecutor()
		{
			if (_factory != null)
			{
				return _factory();
			}
			return default(TExecutor);
		}

		protected void Execute(IPipeline source, IBinarySequence data)
		{
			ICommandResult result;
			var command = (ICommand)data;
			try
			{
				TExecutor executor;
				var key = source.Id;
				lock (_executors)
				{
					if (_executors.ContainsKey(key))
					{
						executor = _executors[key];
					}
					else
					{
						executor = GetExecutor();
						_executors[key] = executor;
					}
				}
				var method = executor.GetType().GetMethod(command.OperationName);
				Type delegateType;
				var typeArgs = method.GetParameters().Select(p => p.ParameterType).ToList();
				if (method.ReturnType == typeof(void))
				{
					delegateType = Expression.GetActionType(typeArgs.ToArray());
				}
				else
				{
					typeArgs.Add(method.ReturnType);
					delegateType = Expression.GetFuncType(typeArgs.ToArray());
				}
				var objDelegate = Delegate.CreateDelegate(delegateType, executor, method);
				object returnValue;
				returnValue = objDelegate.DynamicInvoke(command.Parameters.Values.ToArray());
				if (method.ReturnType == typeof(void))
				{
					result = command.Return();
				}
				else
				{
					result = command.Return(returnValue);
				}
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException)
				{
					result = command.Throw(ex.InnerException);
				}
				else
				{
					result = command.Throw(ex);
				}
			}
			source.Send(result);
		}

		protected ICommand Deserialize(byte[] bytes)
		{
			return BinarySequence.FromBytes<XmlCommand>(bytes);
		}

	}
}
