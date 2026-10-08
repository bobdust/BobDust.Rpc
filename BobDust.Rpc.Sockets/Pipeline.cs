using BobDust.Core.Threading;
using System.Collections.Concurrent;
using BobDust.Core.ExceptionHandling;
using BobDust.Rpc.Sockets.Abstractions;

namespace BobDust.Rpc.Sockets
{
	abstract class Pipeline : ExceptionHandler, IPipeline
	{
		protected const int BufferSize = 8192;
		private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

		private Runnable _receivingTask;
		private ConcurrentDictionary<Guid, ConcurrentQueue<Package>> _receivingQueues;

		public Func<IPipeline, IBinarySequence, Guid, Task>? OnReceived { get; set; }

		public string Id { get; private set; }

		protected Pipeline()
		{
			Id = Guid.NewGuid().ToString();

			_receivingQueues = new ConcurrentDictionary<Guid, ConcurrentQueue<Package>>();
            _receivingTask = new Runnable(async (cancellationToken) =>
            {
                var buffer = new byte[BufferSize];
                var bytesRead = await ReadAsync(buffer, cancellationToken);
                if (bytesRead > 0)
                {
                    buffer = buffer.Take(bytesRead).ToArray();
                    await PackageReceived(buffer);
                }
            })
            {
                OnException = Handle
            };
        }

		private async Task PackageReceived(Package package)
		{
			var token = package.Token;
			lock (_receivingQueues)
			{
				if (!_receivingQueues.ContainsKey(token))
				{
					_receivingQueues[token] = new ConcurrentQueue<Package>();
				}
			}
			var receivingQueue = _receivingQueues[token];
			await _semaphore.WaitAsync();
			try
			{
				receivingQueue.Enqueue(package);
				if (receivingQueue.Count == package.Count)
				{
					await DataReceived(token);
				}
			}
			finally
			{
				_semaphore.Release();
			}
		}

		protected virtual async Task DataReceived(Guid token)
		{
			if (OnReceived != null)
			{
				var data = Receive(token);
				if (data != null)
				{
					await OnReceived(this, data, token);
				}
			}
		}

		public virtual void Open()
		{
			_receivingTask.Start();
		}

		public virtual void Close()
		{
			_receivingTask.Stop();
			_receivingQueues.Clear();
		}

		public virtual void Dispose()
		{
			Close();
		}

		public abstract void Write(byte[] buffer);

		public abstract Task WriteAsync(byte[] buffer, CancellationToken cancellationToken);

		public abstract int Read(byte[] buffer);

		public abstract Task<int> ReadAsync(byte[] buffer, CancellationToken cancellationToken);

		public void Send(IBinarySequence data, Guid token)
		{
			var bytes = data.ToBytes();
			foreach(var package in Package.Split(bytes, token, BufferSize))
			{
				Write(package.ToBytes());
			}
		}

		public async Task SendAsync(IBinarySequence data, Guid token, CancellationToken cancellationToken = default)
		{
			var bytes = data.ToBytes();
			await foreach(var package in Package.SplitAsync(bytes, token, BufferSize))
			{
				await WriteAsync(package.ToBytes(), cancellationToken);
			}
		}

		protected IBinarySequence? Receive(Guid token)
		{
			if (_receivingQueues.TryRemove(token, out var receivingQueue))
			{
				var package = Package.Join(receivingQueue);
				return Deserialize(package.Data!);
			}
			return null;
		}

		protected abstract IBinarySequence Deserialize(byte[] bytes);

	}
}
