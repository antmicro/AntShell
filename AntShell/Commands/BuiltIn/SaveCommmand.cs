/*
Copyright (c) 2013 Ant Micro <www.antmicro.com>

Authors:
* Mateusz Holenko (mholenko@antmicro.com)

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to
permit persons to whom the Software is furnished to do so, subject to
the following conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System.Linq;

namespace AntShell.Commands.BuiltIn
{
	public class SaveCommand : ICommand
	{
		private CommandHistory history;

		public SaveCommand(CommandHistory h)
		{
			history = h;
		}

		#region ICommand implementation

		public int Execute(string[] args, ICommandInteraction writer)
		{
			history.RemoveLast();

			if (args.Length < 2)
			{
				writer.WriteError("History file name is required.");
				return 1;
			}

			int? from = null, length = null;
			if (args.Length > 2)
			{
				int _from;
				if (int.TryParse(args[2], out _from))
				{
					from = _from - 1;
				}
			}
			if (args.Length > 3)
			{
				int _length;
				if (int.TryParse(args[3], out _length))
				{
					length = _length;
				}
			}

			System.IO.File.WriteAllLines(args[1], history.Items.Skip(from ?? 0).Take(length ?? int.MaxValue));

			return 0;
		}

		public string Name { get { return "save"; } }
		public string Description { get { return "Saves commands history to the file"; } }

		#endregion
	}
}

