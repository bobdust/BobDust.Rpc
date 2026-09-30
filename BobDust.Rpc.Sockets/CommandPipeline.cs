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

		protected override async Task DataReceived(Guid token)
		{
			var context = ChannelContext.Get(token);
			if (context == null)
			{
				var asyncContext = AsyncContext.Get(token);
				if (asyncContext != null)
				{
					asyncContext.Set();
				}
				else // server-side pipeline
				{
					using (_ = ChannelContext.New(token))
					{
						await base.DataReceived(token);
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
				Send(request, context.Token);
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
				await context.WaitAsync(MillisecondsTimeout);
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

			private readonly SemaphoreSlim _signal = new(0, 1);

			static AsyncContext()
			{
				_contexts = new ConcurrentDictionary<Guid, AsyncContext>();
			}

			private AsyncContext(Guid token)
			{
				Token = token;
				_signal = new SemaphoreSlim(0, 1);
				CancellationToken = new CancellationToken();
			}

			public Guid Token { get; private set; }
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
				_signal.Release();
				_signal.Dispose();
				_contexts.TryRemove(Token, out var _);
			}

			public Task WaitAsync(int millisecondsTimeout) => _signal.WaitAsync(millisecondsTimeout);

			public void Set()
			{
				if (_signal.CurrentCount == 0)
				{
					_signal.Release();
				}
			}
        }
	}
}
