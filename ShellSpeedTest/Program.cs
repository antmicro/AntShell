using System;
using Mono.Unix.Native;
using Mono.Unix;
using System.Diagnostics;

namespace ShellSpeedTest
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var master = Syscall.open("/proc/self/fd/1", OpenFlags.O_RDWR);
			var stream = new UnixStream(master, true);

			byte[] arr = new byte[] { (byte)27, (byte)'[', (byte)'6', (byte)'n' };

			var stpw = Stopwatch.StartNew();

			for (int i = 0; i < 100; i++)
				stream.Write(arr, 0, 4);

			stpw.Stop();

			Console.WriteLine();
			Console.WriteLine("Time elapsed: {0}ms", stpw.ElapsedMilliseconds);
		}
	}
}
