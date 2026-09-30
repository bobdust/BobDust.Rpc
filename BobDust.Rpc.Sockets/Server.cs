using System.Net.Sockets;
using System.Net;
using BobDust.Core.Threading;
using System.Collections.Concurrent;
using BobDust.Core.ExceptionHandling;
using System.Linq.Expressions;
using System.Reflection;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets
{
	public abstract class Server<TExecutor> : ExceptionHandler, IServer<TExecutor> where TExecutor : class
	{
		private readonly TcpListener _listener;
		private readonly Runnable _listenTask;
		private readonly ConcurrentBag<IPipeline> _pipelines;
		private readonly AutoResetEvent _waitHandle;
		private bool _isStopped;
		private readonly ConcurrentDictionary<string, TExecutor> _executors;
		private readonly Func<TExecutor> _factory;
		private readonly Func<byte[], ICommand> _commandFactory;

		protected Server(int port)
		{
			_listener = new TcpListener(IPAddress.Any, port);
			_listenTask = new Runnable(Listen);
			_pipelines = new ConcurrentBag<IPipeline>();
			_waitHandle = new AutoResetEvent(false);
			_executors = new ConcurrentDictionary<string, TExecutor>();
		}

		protected Server(int port, Func<TExecutor> factory, Func<byte[], ICommand> commandFactory)
		   : this(port)
		{
			_factory = factory;
			_commandFactory = commandFactory;
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

		private async Task Listen(CancellationToken cancellationToken)
		{
			var client = await _listener.AcceptTcpClientAsync(cancellationToken);
			var sendSocket = await _listener.AcceptSocketAsync(cancellationToken);
			var receiveSocket = client.Client;
            var pipeline = new CommandPipeline(new SocketPipeline(sendSocket, receiveSocket), Deserialize)
            {
                OnReceived = Execute,
                OnException = Handle
            };
            pipeline.Open();
			_pipelines.Add(pipeline);
		}

		protected virtual TExecutor GetExecutor()
		{
			if (_factory != null)
			{
				return _factory();
			}

			throw new NotSupportedException("No factory provided for executor creation.");
		}

		protected async Task Execute(IPipeline source, IBinarySequence data, Guid token)
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
				var typeArgs = command.Parameters.Select(p => p.Value.GetType());
				var method = executor.GetType().GetMethod(command.OperationName, typeArgs.ToArray());
				Type delegateType;
				var returnType = method?.ReturnType!;
				if (returnType == typeof(void))
				{
					delegateType = Expression.GetActionType(typeArgs.ToArray());
				}
				else
				{
					delegateType = Expression.GetFuncType(typeArgs.Concat(new[] { returnType }).ToArray());
				}
				var objDelegate = Delegate.CreateDelegate(delegateType, executor, method!);
				object? returnValue = objDelegate.DynamicInvoke(command.Parameters.Select(p => p.Value).ToArray());
				if (returnType == typeof(void))
				{
					result = command.Return();
				}
				else if (returnType == typeof(Task) && returnValue is Task task)
				{
					await task;
					result = command.Return();
				}
				else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>) && returnValue is Task returnTask)
				{
					await returnTask;
					var resultProperty = returnTask.GetType().GetProperty("Result");
					var taskResult = resultProperty?.GetValue(returnTask);
					result = command.Return(taskResult);
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
			source.Send(result, token);
		}

		protected ICommand Deserialize(byte[] bytes)
		{
			return _commandFactory(bytes);
		}

	}
}
