using System;
using System.Threading;
using BobDust.Core.ExceptionHandling;

namespace BobDust.Core.Threading
{
	public class Runnable : ExceptionHandler
	{
		//private Thread _thread;
		private Action _handler;
		private object _lock;
		private ThreadState _state;

		public Runnable(Action handler)
		{
			_lock = new object();
			_handler = handler;
			//_thread = new Thread(new ThreadStart(Run));
		}

		public void Start()
		{
			lock (_lock)
			{
				_state = ThreadState.Running;
			}
			//_thread.Start();
			Action run = Run;
			run.BeginInvoke((asyncResult) =>
			{
				run.EndInvoke(asyncResult);
			}, null);
		}

		private void Run()
		{
			try
			{
				var state = _state;
				while (state == ThreadState.Running)
				{
					_handler();
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
