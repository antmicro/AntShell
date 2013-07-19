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
using System;
using System.Linq;

namespace AntShell.Commands.BuiltIn
{
	public class HistoryCommand : ICommand
	{
		private CommandHistory history;

		public HistoryCommand(CommandHistory h)
		{
			history = h;
		}

		#region ICommand implementation

		public int Execute(string[] args, ICommandInteraction writer)
		{
			writer.WriteLine("Commands history:");
            
            writer.WriteLine();
			var counter = 1;
			foreach (var item in history.Items) 
			{
				writer.WriteLine(string.Format(" {0}: {1}", counter++, item));
			}

			writer.WriteLine();

			return 0;
		}

		public string Name { get { return "history";	} }
		public string Description { get { return "Prints command history";	} }

		#endregion
	}
}

