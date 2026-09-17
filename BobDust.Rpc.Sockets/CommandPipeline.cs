using System.Collections.Concurrent;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets
{
	class CommandPipeline : PipelineDecorator
	{
		protected const int MillisecondsTimeout = 60 * 1000;

		private Func<byte[], IBinarySequence> _deserialize;

		public CommandPipeline(IPipeline pipeline, Func<byte[], IBinarySequence> deserializer)
		   : base(pipeline)
		{
			_deserialize = deserializer;
		}

		protected override IBinarySequence Deserialize(byte[] bytes)
		{
			return _deserialize(bytes);
		}

		protected override Guid CreateDataToken()
		{
			return ChannelContext.CurrentThreadContext.Token;
		}

		protected override void DataReceived(Guid token)
		{
			var context = ChannelContext.Get(token);
			if (context == null)
			{
				var asyncContext = AsyncContext.Get(token);
				if (asyncContext != null)
				{
					asyncContext.WaitHandle.Set();
					//asyncContext.WaitHandle.Release();
				}
				else // server-side pipeline
				{
					using (_ = ChannelContext.New(token))
					{
						base.DataReceived(token);
					}
				}
			}
			else
			{
				context.WaitHandle.Set();
			}
		}

		public IBinarySequence Post(IBinarySequence request)
		{
			using (var context = ChannelContext.New())
			{
				Send(request);
				context.WaitHandle.WaitOne(MillisecondsTimeout);
				var response = Receive(context.Token);
				if (response == null)
				{
					throw new TimeoutException();
				}
				return response;
			}
		}

		public async Task<IBinarySequence> PostAsync(IBinarySequence request)
		{
			await using (var context = AsyncContext.New())
			{
				await SendAsync(request, context.Token, context.CancellationToken);
				context.WaitHandle.WaitOne(MillisecondsTimeout);
				var response = Receive(context.Token);
				if (response == null)
				{
					throw new TimeoutException();
				}
				return response;
			}
		}

		class ChannelContext : IDisposable
		{
			private static ConcurrentDictionary<Guid, ChannelContext> _contexts;

			static ChannelContext()
			{
				_contexts = new ConcurrentDictionary<Guid, ChannelContext>();
			}

			private ChannelContext(Guid token)
			{
				Token = token;
				WaitHandle = new AutoResetEvent(false);
			}

			public Guid Token { get; private set; }
			public AutoResetEvent WaitHandle { get; private set; }

			public static ChannelContext CurrentThreadContext
			{
				get
				{
					var threadId = Thread.CurrentThread.ManagedThreadId;
					var context = AppDomain.CurrentDomain.GetData(threadId.ToString());
					if (context == null)
					{
						throw new InvalidOperationException("No channel context is associated with the current thread.");
					}
					return (ChannelContext)context;
				}
				private set
				{
					AppDomain.CurrentDomain.SetData(Thread.CurrentThread.ManagedThreadId.ToString(), value);
				}
			}

			public static ChannelContext? Get(Guid token)
			{
				if (_contexts.ContainsKey(token))
				{
					return _contexts[token];
				}
				return null;
			}

			public static ChannelContext New()
			{
				return New(Guid.NewGuid());
			}

			public static ChannelContext New(Guid token)
			{
				var context = new ChannelContext(token);
				_contexts[token] = context;
				CurrentThreadContext = context;
				return context;
			}

			public void Dispose()
			{
				WaitHandle.Set();
				WaitHandle.Dispose();
				_contexts.TryRemove(Token, out var _);
			}
		}
		class AsyncContext : IAsyncDisposable
		{
			private static ConcurrentDictionary<Guid, AsyncContext> _contexts;

			static AsyncContext()
			{
				_contexts = new ConcurrentDictionary<Guid, AsyncContext>();
			}

			private AsyncContext(Guid token)
			{
				Token = token;
				WaitHandle = new AutoResetEvent(false);
				//WaitHandle = new SemaphoreSlim(0, 1);
				CancellationToken = new CancellationToken();
			}

			public Guid Token { get; private set; }
			public AutoResetEvent WaitHandle { get; private set; }
			//public SemaphoreSlim WaitHandle { get; private set; }
			public CancellationToken CancellationToken { get; private set; }

			public static AsyncContext? Get(Guid token)
			{
				if (_contexts.ContainsKey(token))
				{
					return _contexts[token];
				}
				return null;
			}

			public static AsyncContext New()
			{
				return New(Guid.NewGuid());
			}

			public static AsyncContext New(Guid token)
			{
				var context = new AsyncContext(token);
				_contexts[token] = context;
				return context;
			}

            public async ValueTask DisposeAsync()
            {
				WaitHandle.Set();
				//WaitHandle.Release();
				WaitHandle.Dispose();
				_contexts.TryRemove(Token, out var _);
			}
        }
	}
}
