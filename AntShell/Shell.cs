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
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using AntShell.Terminal;
using AntShell.Commands;
using AntShell.Commands.BuiltIn;
using AntShell.Helpers;

namespace AntShell
{
	public class Shell : ICommandHandler
	{
		private TerminalEmulator term;
		private CommandHistory history;
		private CommandLine line;

		private List<ICommand> commands;

		public string StartupCommand { get; set; }
		public CommandInteraction Writer { get; private set; }

		private ICommandHandler externalHandler;

		private ShellSettings settings;

		public Shell(Stream s, ShellSettings settings)
		{
			term = new TerminalEmulator(s);
			history = new CommandHistory();
			commands = new List<ICommand>();

			Writer = new CommandInteraction(term);

			line = new CommandLine(term, history, this);

			line.NormalPrompt = settings.NormalPrompt;
			line.SearchPrompt = settings.SearchPrompt ?? new SearchPrompt("search `{0}`> ", ConsoleColor.Yellow);

			this.settings = settings;

			Commands();
		}

		public Shell(Stream s, ICommandHandler handler, ShellSettings settings) : this(s, settings)
		{
			externalHandler = handler;
		}

		public void Start()
		{
			term.Start();

			if (settings.Banner != null)
			{
				term.Write(settings.Banner, false);
				term.NewLine();
				term.NewLine();
			}
			if (StartupCommand != null)
			{
				term.Write(string.Format("Executing startup command: {0}", StartupCommand), false);
				term.NewLine();
				HandleCommand(StartupCommand, null);
				term.NewLine();
			}

			line.Start();

			term.Run();
		}

		public void Reset()
		{
			term.ClearScreen();
			line.CurrentPrompt.Write(term);
		}

		private void Commands()
		{
			RegisterCommand(new CommandFromHistoryCommand(history));
			RegisterCommand(new HistoryCommand(history));
			RegisterCommand(new SaveCommand(history));

			if (settings.UseBuiltinQuit)
			{
				RegisterCommand(new QuitCommand());
			}

			if (settings.UseBuiltinHelp)
			{
				RegisterCommand(new HelpCommand(commands));
			}
		}

		public void RegisterCommand(ICommand cmd)
		{
			if (commands.Any(x => x.Name == cmd.Name))
			{
				throw new ArgumentException("Command name is already registered");
			}

			if (cmd is ICommandWithShortcut && commands.Where(x => x is ICommandWithShortcut).Cast<ICommandWithShortcut>().Any(y => y.Shortcut == ((ICommandWithShortcut)cmd).Shortcut))
			{
				throw new ArgumentException("Command shortcut is already registered");
			}

			commands.Add(cmd);
		}

		string HandleOnHistorySearch(bool reset, string arg)
		{
			if (reset)
			{
				history.Reset();
			}

			return history.ReverseSearch(arg);
		}

		public string[] SuggestionNeeded(string arg)
		{
			var result = commands.Where(x => x.Name.StartsWith(arg)).Select(x => x.Name).ToList();
			if (externalHandler != null)
			{
				result.AddRange(externalHandler.SuggestionNeeded(arg));
			}
			return result.ToArray();
		}

		public string BestSuggestionNeeded(string str)
		{
			var result = commands.Where(x => x.Name.StartsWith(str)).Select(x => x.Name).ToList();
			if (externalHandler != null)
			{
				var bestsug = externalHandler.BestSuggestionNeeded(str);
				if (bestsug != null)
				{
					result.Add(bestsug);
				}
			}
			return Helper.CommonPrefix(result) ?? str;
		}

		public ICommandInteraction HandleCommand(string cmd, ICommandInteraction ic)
		{
			if (cmd != null)
			{
				history.Add(cmd);
			}

			var param = Regex.Matches(cmd, string.Format(@"(?<match>[{0}]+)|\""(?<match>[{0}]*)""", @"\w\.\-\?\!"))
					.Cast<Match>()
					.Select(m => m.Groups["match"].Value)
					.ToArray();

			var command = param.Length > 0 ? commands.SingleOrDefault(x => 
			    (x.Name == param[0]) || 
				((x is ICommandWithShortcut) ? ((ICommandWithShortcut)x).Shortcut == param[0] : false) ||
			 	((x is IOperator) ? ((IOperator)x).Operator == param[0][0] : false)
			) : null;

			if (command == null)
			{
				if (externalHandler != null)
				{
					return externalHandler.HandleCommand(cmd, Writer);
				}

				Writer.WriteError(string.Format("Command {0} not found", param.Length > 0 ? param[0] : cmd));
			}
			else
			{
				if (command is IOperator)
				{
					var list = new List<string> { param[0][0].ToString(), param[0].Substring(1) };
					list.AddRange(param.Skip(1));
					param = list.ToArray();
				}

				command.Execute(param, Writer);
			}

			return Writer;
		}

		string HandleOnHistoryNeeded(int direction)
		{
			if (direction < 0)
			{
				return history.PreviousCommand();
			}
			else if (direction > 0)
			{
				return history.NextCommand();
			}
			else
			{
				return null;
			}
		}

		public void SetPrompt(Prompt p)
		{
			if (p == null)
			{
				line.NormalPrompt = settings.NormalPrompt;
			}
			else
			{
				line.NormalPrompt = p;
			}
		}
	}
}

