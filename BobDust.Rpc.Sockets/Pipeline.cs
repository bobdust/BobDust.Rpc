using System;
using System.Linq;
using BobDust.Core.Threading;
using System.IO;
using System.Collections.Concurrent;
using BobDust.Core.ExceptionHandling;

namespace BobDust.Rpc.Sockets
{
	public abstract class Pipeline : ExceptionHandler, IPipeline
	{
		protected const int BufferSize = 8192;

		private Runnable _receivingTask;
		private ConcurrentDictionary<Guid, ConcurrentQueue<Package>> _receivingQueues;

		public Action<IPipeline, IBinarySequence> OnReceived { get; set; }

		public string Id { get; private set; }

		protected Pipeline()
		{
			Id = Guid.NewGuid().ToString();

			_receivingQueues = new ConcurrentDictionary<Guid, ConcurrentQueue<Package>>();
			_receivingTask = new Runnable(delegate ()
			{
				var buffer = new byte[BufferSize];
				var bytesRead = Read(buffer);
				if (bytesRead > 0)
				{
					buffer = buffer.Take(bytesRead).ToArray();
					Action<Package> packageReceived = PackageReceived;
					packageReceived.BeginInvoke(buffer, (asyncResult) =>
					{
						packageReceived.EndInvoke(asyncResult);
					}, null);
				}
			});
			_receivingTask.OnException = Handle;
		}

		private void PackageReceived(Package package)
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
			lock (receivingQueue)
			{
				receivingQueue.Enqueue(package);
				if (receivingQueue.Count == package.Count)
				{
					DataReceived(token);
				}
			}
		}

		protected virtual void DataReceived(Guid token)
		{
			if (OnReceived != null)
			{
				var data = Receive(token);
				if (data != null)
				{
					OnReceived(this, data);
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

		public abstract int Read(byte[] buffer);

		public void Send(IBinarySequence data)
		{
			var bytes = data.ToBytes();
			var dataSize = BufferSize - Package.HeaderSize;
			var length = bytes.Length;
			var count = length / dataSize + (length % dataSize > 0 ? 1 : 0);
			var token = CreateDataToken();
			using (var stream = new MemoryStream(bytes))
			{
				using (var reader = new BinaryReader(stream))
				{
					for (var index = 1; index <= count; index++)
					{
						var remainLength = (int)(stream.Length - stream.Position);
						var bytesCount = remainLength > dataSize ? dataSize : remainLength;
						var packageData = reader.ReadBytes(bytesCount);
						var package = new Package(token, index, count, packageData);
						Write(package.ToBytes());
					}
				}
			}
		}

		protected virtual Guid CreateDataToken()
		{
			var token = Guid.NewGuid();
			return token;
		}

		protected IBinarySequence Receive(Guid token)
		{
			ConcurrentQueue<Package> receivingQueue;
			if (_receivingQueues.TryRemove(token, out receivingQueue))
			{
				var package = Package.Join(receivingQueue);
				return Deserialize(package.Data);
			}
			return null;
		}

		protected abstract IBinarySequence Deserialize(byte[] bytes);

	}
}
