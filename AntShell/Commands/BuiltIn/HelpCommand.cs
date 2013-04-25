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
using System.Collections.Generic;

namespace AntShell.Commands.BuiltIn
{
	public class HelpCommand : ICommandWithShortcut
	{
		#region ICommand implementation

		public int Execute(string[] args, ICommandInteraction writer)
		{
			writer.WriteLine("\nCommands:");
			writer.WriteLine(string.Format("{0,-18}|{1}", "Name", "Description"));
			writer.WriteLine("=======================================");
			foreach(var item in commands)
			{
				writer.WriteLine(string.Format("{0,-18}: {1}", item.Name, item.Description));    
			}

			return 0;
		}

		public string Name { get { return "help"; }	}
		public string Description { get { return "Show this table"; }	}

		#endregion

		#region ICommandWithShortcut implementation

		public string Shortcut { get { return "?"; } }

		#endregion

		public HelpCommand(IEnumerable<ICommand> commands)
		{
			this.commands = commands;
		}

		private IEnumerable<ICommand> commands;
	}
}

