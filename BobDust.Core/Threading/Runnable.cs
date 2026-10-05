using BobDust.Core.ExceptionHandling;

namespace BobDust.Core.Threading
{
	public class Runnable : ExceptionHandler
	{
		private Func<CancellationToken, Task> _handler;
		private object _lock;
		public ThreadState State { get; private set; } = ThreadState.Unstarted;
		private CancellationTokenSource _cancellationTokenSource;

		public Runnable(Func<CancellationToken, Task> handler)
		{
			_lock = new object();
			_handler = handler;
			_cancellationTokenSource = new CancellationTokenSource();
		}

		public void Start()
		{
			lock (_lock)
			{
				State = ThreadState.Running;
			}
			Task.Run(Run);
		}

		private async Task Run()
		{
			try
			{
				var state = State;
				var cancellationToken = _cancellationTokenSource.Token;
				while (state == ThreadState.Running && !cancellationToken.IsCancellationRequested)
				{
					await _handler(cancellationToken);
					lock (_lock)
					{
						state = State;
					}
				}
			}
			catch (Exception ex)
			{
				Handle(ex, this);
			}
		}

		public void Stop()
		{
			lock (_lock)
			{
				State = ThreadState.Stopped;
			}
			_cancellationTokenSource.Cancel();
		}

	}
}
