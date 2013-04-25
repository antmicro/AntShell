
using System;
using System.Timers;
using System.Collections.Generic;

namespace Extensions
{
	public class TimerQueue<T>
	{
		public event Action OnTimeout;

		private Timer Timer = new Timer();
		private int milliseconds;
		private List<T> queue = new List<T>();
		private bool started = false;

		public TimerQueue(int milliseconds)
		{
			this.milliseconds = milliseconds;
			RecreateTimer();
		}

		public void Enqueue(T b)
		{
			Start();
			queue.Add(b);
		}

		public T Dequeue()
		{
			if (queue.Count == 0)
			{
				return default(T);
			}

			var result = queue[0];
			queue.RemoveAt(0);
			return result;
		}

		public void Clear()
		{
			Stop();
			queue.Clear();
		}

		public void Skip(int length)
		{
			var i = length;
			while (i > 0 && queue.Count > 0)
			{
				queue.RemoveAt(0);
				i--;
			}
		}

		public T[] Value 
		{
			get
			{
				Pause();
				return queue.ToArray();
			}
		}

		public bool IsRunning
		{
			get { return started; }
		}

		public int Size
		{
			get { return queue.Count; }
		}

		private void Timeout(object sender, ElapsedEventArgs e)
		{
			Timer.Stop();
			var ot = OnTimeout;
			if (ot != null)
			{
				ot();
			}
			started = false;
		}

		#region Timer control

		private void Start()
		{
			if (!started)
			{
				Timer.Start();
				started = true;
			}
		}

		private void Pause()
		{
			if (started && Timer.Enabled)
			{
				Timer.Stop();
			}
		}

		public void Resume()
		{
			if (started && !Timer.Enabled)
			{
				Timer.Start();
			}
		}

		public void Stop()
		{
			if (started)
			{
				Timer.Stop();
				started = false;
			}
		}

		#endregion

		private void RecreateTimer()
		{
			//Timer = new Timer(Timeout);
			Timer.Interval = milliseconds;
			Timer.Elapsed += Timeout;
		}
	}
}

