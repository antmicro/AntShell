using System;
using Mono.Unix.Native;
using Mono.Unix;
using System.Diagnostics;
using AntShellDemo;

namespace ShellSpeedTest
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			var pty = new Pty("/proc/self/fd/1");
			var stream = pty.Stream;

			byte[] arr = new byte[] { (byte)27, (byte)'[', (byte)'6', (byte)'n' };

			var stpw = Stopwatch.StartNew();

			for (int i = 0; i < 100; i++)
			{
				stream.Write(arr, 0, 4);

				stream.ReadByte(); // ESC
				stream.ReadByte(); // [

				char c;
				do {
					c = (char)stream.ReadByte();
				} while (c != ';');

				do {
					c = (char)stream.ReadByte();
				} while (c != 'R');
			}

			stpw.Stop();

			Console.WriteLine();
			Console.WriteLine("Time elapsed: {0}ms", stpw.ElapsedMilliseconds);
		}
	}
}
