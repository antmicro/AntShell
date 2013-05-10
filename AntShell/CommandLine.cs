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
using AntShell.Helpers;
using AntShell.Terminal;

namespace AntShell
{
	public class CommandLine : ITerminalHandler
	{
		#region Private fields

		private Mode mode;
		private CommandEditor command;
		private CommandEditor search;
		private TerminalEmulator terminal;

		public Prompt NormalPrompt { get; set; }

		private SearchPrompt _searchPrompt;
		public SearchPrompt SearchPrompt 
		{ 
			get { return _searchPrompt; }
			set { _searchPrompt = value; _searchPrompt.SetCommandEditor(search); }
		}

		private CommandHistory history;
		private ICommandHandler handler;

		private bool tabTabMode = false;

		#endregion

		public CommandLine(TerminalEmulator term, CommandHistory history, ICommandHandler handler)
		{
			this.handler = handler;
			terminal = term;
			this.history = history;
			command = new CommandEditor();
			search = new CommandEditor();

			term.Handler = this;
		}

		public void Start()
		{
			CurrentPrompt.Write(terminal);
		}

		private CommandEditor CurrentEditor
		{
			get
			{
				return mode == Mode.Command ? command : search;
			}
		}

		internal Prompt CurrentPrompt
		{
			get
			{
				return mode == Mode.Command ? NormalPrompt : SearchPrompt;
			}
		}

		#region Input handlers

		public void HandleControlSequence(ControlSequence seq)
		{
			switch (seq.Type) {
			case ControlSequenceType.Home:
			{
				var diff = CurrentEditor.MoveHome();
				terminal.CursorBackward(diff);
			}
				break;

			case ControlSequenceType.End:
			{
				var diff = CurrentEditor.MoveEnd();
				terminal.CursorForward(diff);
			}
				break;

			case ControlSequenceType.LeftArrow:
				if (CurrentEditor.MoveCharacterBackward())
				{
					terminal.CursorBackward();
				}
				break;

			case ControlSequenceType.RightArrow:
				if (CurrentEditor.MoveCharacterForward())
				{
					terminal.CursorForward();
				}
				break;

			case ControlSequenceType.UpArrow:
			{
				if (!history.HasMoved)
				{
					history.SetCurrentCommand(CurrentEditor.Value);
				}

				var prev = history.PreviousCommand();
				if (prev != null)
				{
					var len = CurrentEditor.Position;
					CurrentEditor.SetValue(prev);
					terminal.CursorBackward(len);
					terminal.ClearToTheEndOfLine();
					terminal.Write(CurrentEditor.Value);
				}
			}
				break;

			case ControlSequenceType.DownArrow:
			{
				var cmd = history.NextCommand();
				if (cmd != null)
				{
					var len = CurrentEditor.Position;
					CurrentEditor.SetValue(cmd);
					terminal.CursorBackward(len);
					terminal.ClearToTheEndOfLine();
					terminal.Write(CurrentEditor.Value);
				}
			}
				break;

			case ControlSequenceType.Backspace:
				if (CurrentEditor.RemovePreviousCharacter())
				{
					terminal.CursorBackward();

					if (mode == Mode.Command)
					{
						terminal.ClearToTheEndOfLine();
						terminal.WriteNoMove(CurrentEditor.ToString(CurrentEditor.Position));
					}
					else
					{
						SearchPrompt.Recreate(terminal);

						history.Reset();
						var result = history.ReverseSearch(search.Value);
						terminal.WriteNoMove(result, SearchPrompt.Skip);
					}
				}
				break;

			case ControlSequenceType.Delete:
				if (CurrentEditor.RemoveNextCharacter())
				{
					if (mode == Mode.Command)
					{
						terminal.ClearToTheEndOfLine();
						terminal.WriteNoMove(CurrentEditor.ToString(CurrentEditor.Position));
					}
					else
					{
						SearchPrompt.Recreate(terminal);

						history.Reset();
						var result = history.ReverseSearch(search.Value);
						terminal.WriteNoMove(result, SearchPrompt.Skip);
					}
				}
				break;
			
			case ControlSequenceType.CtrlLeftArrow:
			{
				var diff = CurrentEditor.MoveWordBackward();
				terminal.CursorBackward(diff);
			}
				break;

			case ControlSequenceType.CtrlRightArrow:
			{
				var diff = CurrentEditor.MoveWordForward();
				terminal.CursorForward(diff);
			}
				break;

			case ControlSequenceType.Ctrl:
				switch((char)seq.Argument) {
				case 'c':
					if (mode == Mode.Command)
					{
						var diff = CurrentEditor.MoveEnd();
						terminal.CursorForward(diff);
						terminal.Write("^C");
						terminal.NewLine();
					}
					else
					{
						mode = Mode.Command;
						terminal.ClearLine();
					}

					search.Clear();
					command.Clear();

					NormalPrompt.Write(terminal);
					history.Reset();

					break;

				case 'r':
					if (mode == Mode.Command)
					{
						mode = Mode.Search;
						terminal.CursorBackward(command.MoveHome());
						terminal.ClearLine();
						search.SetValue(command.Value);
						command.Clear();

						CurrentPrompt.Write(terminal);
					}
					else
					{
						if (search.Value != string.Empty)
						{
							var result = history.ReverseSearch(search.Value);
							terminal.CursorForward(SearchPrompt.Skip);
							terminal.ClearToTheEndOfLine();
							terminal.WriteNoMove(result);
							terminal.CursorBackward(SearchPrompt.Skip);
						}
					}

					break;

				case 'w':
				{
					var diff = CurrentEditor.RemoveWord();
					if (diff > 0)
					{
						terminal.CursorBackward(diff);
						if (mode == Mode.Command)
						{
							terminal.ClearToTheEndOfLine();
							terminal.WriteNoMove(CurrentEditor.ToString(CurrentEditor.Position));
						}
						else
						{
							SearchPrompt.Recreate(terminal);
							history.Reset();
							var result = history.ReverseSearch(search.Value);
							terminal.WriteNoMove(result);
						}
					}
				}
					break;

				case 'k':
					CurrentEditor.RemoveToTheEnd();

					if (mode == Mode.Command)
					{
						terminal.ClearToTheEndOfLine();
					}
					else
					{
						SearchPrompt.Recreate(terminal);
						history.Reset();
						var result = history.ReverseSearch(search.Value);
						terminal.WriteNoMove(result);
					}
					break;

				default:
					break;
				}
				break;

			case ControlSequenceType.Esc:
				if (mode == Mode.Search)
				{
					search.Clear();
					command.SetValue(history.CurrentCommand ?? string.Empty);
					mode = Mode.Command;

					terminal.ClearLine();
					NormalPrompt.Write(terminal);
					terminal.Write(command.Value);
				}
				break;

			case ControlSequenceType.Tab:
				if (mode == Mode.Search)
				{
					mode = Mode.Command;
					CurrentEditor.SetValue(history.CurrentCommand);
					terminal.ClearLine();
					CurrentPrompt.Write(terminal);
					terminal.Write(CurrentEditor.Value);
				}
				else
				{
					if (tabTabMode)
					{
						var sugs = handler.SuggestionNeeded(CurrentEditor.Value);
						if (sugs.Length == 1)
						{
							terminal.CursorBackward(CurrentEditor.Length);
							CurrentEditor.SetValue(sugs[0]);
							terminal.Write(CurrentEditor.ToString());
						}
						else if (sugs.Length > 1)
						{
							terminal.NewLine();
							foreach(var sug in sugs)
							{
								terminal.WriteRaw(string.Format(" {0}\r\n", sug), ConsoleColor.Blue);
							}

							CurrentEditor.SetValue(Helper.CommonPrefix(sugs));
							NormalPrompt.Write(terminal);
							terminal.Write(CurrentEditor.Value);
						}
						return;
					}
					else
					{
						tabTabMode = true;
						var sug = handler.BestSuggestionNeeded(CurrentEditor.Value);
						if (sug != null)
						{
							terminal.CursorBackward(CurrentEditor.Length);
							CurrentEditor.SetValue(sug);
							terminal.Write(CurrentEditor.ToString());
						}

						return;
					}
				}

				break;

			case ControlSequenceType.Enter:
				var wasInSearchMode = false;
				if (mode == Mode.Search)
				{
					mode = Mode.Command;
					command.SetValue(history.CurrentCommand ?? string.Empty);
					search.Clear();

					terminal.ClearLine();
					wasInSearchMode = true;
				}

				if (!wasInSearchMode)
				{
					terminal.CursorForward(CurrentEditor.MoveEnd());
					terminal.NewLine();
				}
				
                if (CurrentEditor.Value != string.Empty)
				{
                    var interaction = handler.HandleCommand(CurrentEditor.Value, null);
					if (interaction != null && interaction.QuitEnvironment)
					{
						terminal.Stop();
						return;
					}

					if (wasInSearchMode)
					{
						NormalPrompt.Write(terminal);
						terminal.Write(command.Value);
						terminal.NewLine();
					}

					CurrentEditor.Clear();
					if (interaction != null && interaction.CommandToExecute != null)
					{
						CurrentEditor.SetValue(interaction.CommandToExecute);
						(interaction as CommandInteraction).Clear();
					}
				}

				NormalPrompt.Write(terminal);
				if (CurrentEditor.Length > 0)
				{
					terminal.Write(CurrentEditor.Value);
				}
				history.Reset();
                
				break;

			default:
				Console.WriteLine("WARNING: Unknown control sequence!");
			break;
			}

			tabTabMode = false;
		}

		public void HandleCharacter(char character)
		{
			if (char.IsDigit(character) || char.IsLetter(character) || char.IsSymbol(character) || char.IsPunctuation(character) || char.IsWhiteSpace(character))
			{
				var appended = CurrentEditor.InsertCharacter(character);

				if (mode == Mode.Command)
				{
					terminal.Write(character);

					if (!appended)
					{
						terminal.WriteNoMove(CurrentEditor.ToString(CurrentEditor.Position));
					}
				}
				else
				{
					SearchPrompt.Recreate(terminal, -1);

					history.Reset();
					var result = history.ReverseSearch(search.Value);
					terminal.WriteNoMove(result, SearchPrompt.Skip);
				}
			}

			tabTabMode = false;
		}

		#endregion

		private enum Mode
		{
			Command,
			Search
		}
	}
}
