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
	public abstract class Server : ExceptionHandler, IServer
	{
		private readonly TcpListener _listener;
		private readonly Runnable _listenTask;
		private readonly ConcurrentBag<IPipeline> _pipelines;
		private readonly ConcurrentDictionary<(string Id, string Contract), object> _executors;
		private readonly Func<byte[], ICommand> _commandFactory;
		protected readonly ConcurrentDictionary<string, Func<object>> _factories;

		protected Server(int port, Func<byte[], ICommand> commandFactory)
		{
			_listener = new TcpListener(IPAddress.Any, port);
			_listenTask = new Runnable(Listen);
			_pipelines = new ConcurrentBag<IPipeline>();
			_executors = new ConcurrentDictionary<(string Id, string Contract), object>();
			_commandFactory = commandFactory;
			_factories = new ConcurrentDictionary<string, Func<object>>();
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

		protected virtual object GetExecutor(string contractType)
		{
			if (_factories.TryGetValue(contractType, out var factory))
			{
				return factory();
			}

			throw new NotSupportedException("No factory provided for executor creation.");
		}

		protected async Task Execute(IPipeline source, IBinarySequence data, Guid token)
		{
			ICommandResult result;
			var command = (ICommand)data;
			try
			{
				object executor;
				var contractType = GetContractType(command);
				var key = (source.Id, contractType);
				lock (_executors)
				{
					if (_executors.ContainsKey(key))
					{
						executor = _executors[key];
					}
					else
					{
						executor = GetExecutor(contractType);
						_executors[key] = executor;
					}
				}
				var typeArgs = command.Parameters!.Select(p => p.Value.GetType());
				var method = executor.GetType().GetMethod(command.OperationName!, [.. typeArgs]);
				Type delegateType;
				var returnType = method?.ReturnType!;
				if (returnType == typeof(void))
				{
					delegateType = Expression.GetActionType([.. typeArgs]);
				}
				else
				{
					delegateType = Expression.GetFuncType([.. typeArgs, returnType]);
				}
				var objDelegate = Delegate.CreateDelegate(delegateType, executor, method!);
				object? returnValue = objDelegate.DynamicInvoke([.. command.Parameters!.Select(p => p.Value)]);
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
				if (ex is TargetInvocationException tie && tie is { InnerException: not null })
				{
					result = command.Throw(tie.InnerException);
				}
				else
				{
					result = command.Throw(ex);
				}
			}
			source.Send(result, token);
		}

		protected string GetContractType(ICommand command)
		{
			return command.ContractType!;
		}

		protected ICommand Deserialize(byte[] bytes)
		{
			return _commandFactory(bytes);
		}

		public void Start()
		{
			if (_listenTask.State != ThreadState.Running)
			{
				_listener.Start();
				_listenTask.Start();
			}
		}

		public void Stop()
		{
			foreach (var pipeline in _pipelines)
			{
				pipeline.Close();
			}
			_listenTask.Stop();
			_listener.Server.Close();
			_listener.Stop();
		}

		public IServer Register<TContract, TImplementation>(Func<TImplementation> factory) where TImplementation : class, TContract
		{
			_factories[typeof(TContract).FullName!] = factory;
			return this;
		}
	}

	public abstract class Server<TExecutor> : Server, IServer<TExecutor> where TExecutor : class
	{
		protected Server(int port, Func<byte[], ICommand> commandFactory) : base(port, commandFactory)
		{
		}

		protected Server(int port, Func<TExecutor> factory, Func<byte[], ICommand> commandFactory) : base(port, commandFactory)
		{
			Register<TExecutor, TExecutor>(factory);
		}

		protected override object GetExecutor(string contractType)
		{
			if (!_factories.ContainsKey(contractType))
			{
				return base.GetExecutor(typeof(TExecutor).FullName!);
			}
			return base.GetExecutor(contractType);
		}
	}
}
