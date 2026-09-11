using BobDust.Core.ExceptionHandling;

namespace BobDust.Core.Threading
{
	public class Runnable : ExceptionHandler
	{
		private Func<Task> _handler;
		private object _lock;
		private ThreadState _state;

		public Runnable(Func<Task> handler)
		{
			_lock = new object();
			_handler = handler;
		}

		public void Start()
		{
			lock (_lock)
			{
				_state = ThreadState.Running;
			}
			Task.Run(Run);
		}

		private async Task Run()
		{
			try
			{
				var state = _state;
				while (state == ThreadState.Running)
				{
					await _handler();
					lock (_lock)
					{
						state = _state;
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
				_state = ThreadState.Stopped;
			}
		}

	}
}
