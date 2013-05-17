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
using System.Linq;
using System.IO;
using AntShell.Helpers;
using System.Threading;

namespace AntShell.Terminal
{
	public class TerminalEmulator
	{
		private const int MAX_HEIGHT = 9999;
		private const int MAX_WIDTH = 9999;

		private SequenceValidator validator;
		private Queue<byte> queue;

		public ITerminalHandler Handler { get; set; }

		private VirtualCursor vcursor;

		private List<byte> Buffer;
		private List<int> WrappedLines;
		private int CurrentLine;
		private int LinesScrolled;

		private Stream stream;
        private bool onceAgain;

		public TerminalEmulator(Stream stream)
		{
			validator = new SequenceValidator();
			queue = new Queue<byte>();

			this.stream = stream;
			WrappedLines = new List<int>();
			Buffer = new List<byte>();
			vcursor = new VirtualCursor();

			ControlSequences();
		}

		public void Start()
		{
			ClearScreen();
			ResetColors();
			CursorUp(MAX_HEIGHT, false);
			CursorToColumn(0, false);

			Calibrate();
		}

		private void ControlSequences()
		{
			validator.Add(ControlSequenceType.LeftArrow,  	(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'D');
			validator.Add(ControlSequenceType.RightArrow, 	(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'C');
			validator.Add(ControlSequenceType.UpArrow,    	(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'A');
			validator.Add(ControlSequenceType.DownArrow,  	(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'B');

			validator.Add(ControlSequenceType.CtrlLeftArrow,    (byte)SequenceElement.ESC, (byte)'O', (byte)'D');
			validator.Add(ControlSequenceType.CtrlLeftArrow,    (byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'1', (byte)';', (byte)'5', (byte)'D');
			validator.Add(ControlSequenceType.CtrlRightArrow,   (byte)SequenceElement.ESC, (byte)'O', (byte)'C');
			validator.Add(ControlSequenceType.CtrlRightArrow,   (byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'1', (byte)';', (byte)'5', (byte)'C');

			validator.Add(ControlSequenceType.Delete,		(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'3', (byte)'~');

			validator.Add(ControlSequenceType.Home, 		(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'1', (byte)'~');
			validator.Add(ControlSequenceType.Home,			(byte)SequenceElement.ESC, (byte)'O', (byte)'H');
			validator.Add(ControlSequenceType.Home,			(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'H');

			validator.Add(ControlSequenceType.End, 			(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'4', (byte)'~');
			validator.Add(ControlSequenceType.End,			(byte)SequenceElement.ESC, (byte)'O', (byte)'F');
			validator.Add(ControlSequenceType.End,			(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)'F');

			validator.Add(ControlSequenceType.Esc,			(byte)SequenceElement.ESC, (byte)SequenceElement.ESC);

			validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'k'), 	(byte)11);
			validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'r'), 	(byte)18);
			validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'w'), 	(byte)23);
			validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'c'), 	(byte)3);

			validator.Add(ControlSequenceType.Tab, 			(byte)'\t');
			validator.Add(ControlSequenceType.Backspace, 	(byte)127);
			validator.Add(ControlSequenceType.Enter,		(byte)'\r');

			validator.Add(ControlSequenceType.Ignore,		(byte)12);

			validator.Add((ControlSequenceGenerator)((s, l) => {
				var str = new string(s.Select(x => (char)x).ToArray());
				var trimmed = str.Substring(2, str.Length - 3);
				var splitted = trimmed.Split(';');

				return new ControlSequence(ControlSequenceType.CursorPosition, new Position(int.Parse(splitted[1]), int.Parse(splitted[0])));
			}),
			(byte)SequenceElement.ESC, (byte)SequenceElement.CSI, (byte)SequenceElement.INTEGER, (byte)';', (byte)SequenceElement.INTEGER, (byte)'R');
		}

		#region Input handling

	    public void Stop()
	    {
			ClearScreen();
	      	CursorToColumn(1);
	      	CursorUp(MAX_HEIGHT);
	      	ResetColors();

			onceAgain = false;
			stream.Close();
	    }

		public void Run()
		{
      		onceAgain = true;
			while(onceAgain)
			{
				int value;

				if (Buffer.Count > 0)
				{
					value = Buffer.ElementAt(0);
					Buffer.RemoveAt(0);
				}
				else
				{
					value = stream.ReadByte();
					if (value == -1)
					{
						continue;
					}
				}

				HandleByte((byte)value);	
			}
		}

		private void HandleByte(byte b)
		{
			if (queue.Count == 0)
			{
				if (b == (byte)SequenceElement.ESC)
				{
					queue.Enqueue(b);
					return;
				}
				
				// try special control sequence
				ControlSequence cs;
				var result = validator.Check(new [] {b}, out cs);
				if (result == SequenceValidationResult.SequenceFound)
				{
					Handler.HandleControlSequence(cs);
					return;
				}
				
				// so it must be normal character
				Handler.HandleCharacter((char)b);
			}
			else
			{
				queue.Enqueue(b);
				ControlSequence cs;
				var validationResult = validator.Check(queue.ToArray(), out cs);
				if (cs == null)
				{
					if (validationResult == SequenceValidationResult.SequenceNotFound)
					{
						queue.Clear();
					}
				}
				else
				{
					queue.Clear();
					Handler.HandleControlSequence(cs);
				}
			}
		}

		#endregion
       
		private void OnScreenScroll()
		{
			LinesScrolled++;
		}

		private void OnLineWrapped()
		{
			WrappedLines.Add(vcursor.MaxPosition.X + 1);
			CurrentLine++;
		}

		#region Writers

		internal int Write(string text, bool checkWrap = true, ConsoleColor? color = null)
		{
			var result = 0;

			if (text != null)
			{
				if (color.HasValue)
				{
					SetColor(color.Value);
				}

				foreach(var c in text.ToCharArray())
				{
					result += WriteChar(c, checkWrap) ? 1 : 0;
				}

				if (color.HasValue)
				{
					ResetColors();
				}
			}

			return result;
		}

		public void Write(char c, bool checkWrap = true, ConsoleColor? color = null)
		{
			if (color.HasValue)
			{
				SetColor(color.Value);
			}
			
			WriteChar(c, checkWrap);
			
			if (color.HasValue)
			{
				ResetColors();
			}
		}

		public void WriteNoMove(string text, int skip = 0, ConsoleColor? color = null)
		{
			if (text != null)
			{
				var ep = vcursor.Position;
				var currline = CurrentLine;

				HideCursor();

				//LinesScrolled = 0;

				CursorForward(skip);
				var count = Write(text, true, color);
				CursorBackward(count + skip);

				ShowCursor();

				vcursor.Position = ep;
				CurrentLine = currline;
			}
		}

		public void WriteRaw(char c, ConsoleColor? color = null)
		{
			if (color.HasValue)
			{
				SetColor(color.Value);
			}

			WriteCharRaw(c);

			if (color.HasValue)
			{
				ResetColors();
			}
		}

		public void WriteRaw(string text, ConsoleColor? color = null)
		{
			if (text != null)
			{
				if (color.HasValue)
				{
					SetColor(color.Value);
				}
				
				foreach(var c in text.ToCharArray())
				{
					WriteCharRaw(c);
				}
				
				if (color.HasValue)
				{
					ResetColors();
				}
			}
		}

		private bool InEscapeMode = false;
		private bool WriteChar(char c, bool checkIfWrapped = true)
		{
			WriteCharRaw(c);

			if (checkIfWrapped)
			{

				if (c == (byte)SequenceElement.ESC) // to eliminate control sequences, mostly color change
				{
					InEscapeMode = true;
				}

				if (!InEscapeMode) // if the char changed cursor position by one; check for color steering codes
				{
					var result = vcursor.MoveForward();

					if (vcursor.IsCursorOutOfLine && !vcursor.IsCursorOutOfScreen)
					{
						CursorDown();
						CursorToColumn(1);
					}

					if (result == VirtualCursorMoveResult.LineWrapped)
					{
						OnLineWrapped();
					}

					if (result == VirtualCursorMoveResult.ScreenScrolled)
					{
						OnLineWrapped();
						OnScreenScroll();
					}

					return true;
				}
				else
				{
					if (c == 'm')
					{
						InEscapeMode = false;
					}
				}

			}

			return false;
		}

		private void WriteCharRaw(char c)
		{
			stream.WriteByte((byte)c);
		}

		#endregion

		#region Cursor movement

		public void CursorUp(int n = 1, bool recalculateEstimatedPosition = true)
		{
			if (n > 0)
			{
				SendCSI();
				if (n > 1)
				{
					SendControlSequence(n.ToString());
				}

				SendControlSequence((byte)'A');

				if (recalculateEstimatedPosition)
				{
					vcursor.MoveUp(n);
				}
			}
		}

		public void CursorDown(int n = 1, bool recalculateEstimatedPosition = true)
		{
			if (n > 0)
			{
				SendCSI();
				if (n > 1)
				{
					SendControlSequence(n.ToString());
				}

				SendControlSequence((byte)'B');

				if (recalculateEstimatedPosition)
				{
					vcursor.MoveDown(n);
				}
			}
		}

		public void CursorForward(int n = 1)
		{
			if (n > 0)
			{
				var move = vcursor.CalculateMoveForward(n);

				CursorDown(move.Y);

				n = Math.Abs(move.X);

				if (n != 0)
				{
					SendCSI();
				}
				if (n > 1)
				{
					SendControlSequence(n.ToString());
				}
				if (move.X > 0)
				{
					SendControlSequence((byte)'C');
					vcursor.MoveForward(n, true);
				}
				else if (move.X < 0)
				{
					SendControlSequence((byte)'D');
					vcursor.MoveBackward(n);
				}

				CurrentLine = vcursor.RealPosition.X == 1 ? CurrentLine - (move.Y - 1) : CurrentLine - move.Y;

				if (vcursor.IsCursorOutOfScreen)
				{
					ScrollDown();
				}
			}
		}

		public void CursorBackward(int n = 1)
		{
			if (n > 0)
			{
				var vb = (vcursor.RealPosition.X == 1);
				var move = vcursor.CalculateMoveBackward(n);

				CurrentLine = Math.Max(0, CurrentLine + (vb ? move.Y + 1 : move.Y));

				CursorUp(-move.Y);
				n = Math.Abs(move.X);

				if (n != 0)
				{
					SendCSI();
				}
				if (n > 1)
				{
					SendControlSequence(n.ToString());
				}
				if (move.X < 0)
				{
					SendControlSequence((byte)'D');
					vcursor.MoveBackward(n);
				}
				else if (move.X > 0)
				{
					SendControlSequence((byte)'C');
					vcursor.MoveForward(n, false);
				}
			}
		}

		public void CursorToColumn(int n, bool recalculateEstimatedPosition = true)
		{
			SendCSI();
			SendControlSequence(n.ToString());
			SendControlSequence((byte)'G');

			if (recalculateEstimatedPosition)
			{
				vcursor.SetX(n);
			}
		}

		public void NewLine()
		{
			Write("\n\r", false);
			CurrentLine = 0;
			WrappedLines.Clear();

			vcursor.SetX(1);
			vcursor.MoveDown();
		}

		#endregion

		#region Erase

		public void ClearLine()
		{
			SendCSI((byte)'2', (byte)'K');
			CursorToColumn(0);

			var count = vcursor.MaxReachedPosition.Y - vcursor.RealPosition.Y;
			CursorDown(count);
			for (int i = count; i > 0; i--)
			{
				SendCSI((byte)'2', (byte)'K'); // clear line
				CursorUp();
				WrappedLines.RemoveAt(WrappedLines.Count - i);
			}

			CurrentLine = 0;

			vcursor.MaxReachedPosition.X = vcursor.RealPosition.X;
			vcursor.MaxReachedPosition.Y = vcursor.RealPosition.Y;
		}

		public void ClearToTheEndOfLine()
		{
			var count = vcursor.MaxReachedPosition.Y - vcursor.RealPosition.Y;
			CursorDown(count);
			for (int i = count; i > 0; i--)
			{
				SendCSI((byte)'2', (byte)'K'); // clear line
				CursorUp();		

				if (WrappedLines.Count > 0 && WrappedLines.Count - i > 0)
				{
					WrappedLines.RemoveAt(WrappedLines.Count - i);
				}
			}

			SendCSI((byte)'K');

			vcursor.MaxReachedPosition.X = vcursor.RealPosition.X;
			vcursor.MaxReachedPosition.Y = vcursor.RealPosition.Y;
		}

		public void ClearScreen()
		{
			SendCSI((byte)'2', (byte)'J');
		}

		#endregion

		#region Display

		public void ResetColors()
		{
			SendCSI((byte)'0', (byte)'m'); // reset colors
		}

		public void Calibrate()
		{
			vcursor.Calibrate(GetCursorPosition(true), GetSize());
		}

		public void ScrollDown()
		{
			SendControlSequence((byte)SequenceElement.ESC, (byte)'D');
		}

		public void SaveCursor()
		{
			SendCSI((byte)'s');
		}

		public void RestoreCursor()
		{
			SendCSI((byte)'u');
		}

		public void HideCursor()
		{
			SendCSI();
			SendControlSequence("?25l");
		}

		public void ShowCursor()
		{
			SendCSI();
			SendControlSequence("?25h");
		}

		public void SetColor(ConsoleColor color)
		{
			switch(color)
			{
			case ConsoleColor.Black:
				SendCSI((byte)SequenceElement.SEM, (byte)'0', (byte)'3', (byte)'0', (byte)'m');
				break;
			case ConsoleColor.Red:
				SendCSI((byte)SequenceElement.SEM, (byte)'0', (byte)'3', (byte)'1', (byte)'m');
				break;
			case ConsoleColor.Green:
				SendCSI((byte)SequenceElement.SEM, (byte)'0', (byte)'3', (byte)'2', (byte)'m');
				break;
			case ConsoleColor.Yellow:
				SendCSI((byte)SequenceElement.SEM, (byte)'0', (byte)'3', (byte)'3', (byte)'m');
				break;
			case ConsoleColor.Blue:
				SendCSI((byte)SequenceElement.SEM, (byte)'0', (byte)'3', (byte)'4', (byte)'m');
				break;
			case ConsoleColor.Magenta:
				SendCSI((byte)SequenceElement.SEM, (byte)'0', (byte)'3', (byte)'5', (byte)'m');
				break;
			case ConsoleColor.Cyan:
				SendCSI((byte)SequenceElement.SEM, (byte)'0', (byte)'3', (byte)'6', (byte)'m');
				break;
			case ConsoleColor.Gray:
				SendCSI((byte)SequenceElement.SEM, (byte)'0', (byte)'3', (byte)'7', (byte)'m');
				break;

			case ConsoleColor.DarkGray:
				SendCSI((byte)'3', (byte)'0', (byte)SequenceElement.SEM, (byte)'1', (byte)'m');
				break;
			case ConsoleColor.DarkRed:
				SendCSI((byte)'3', (byte)'1', (byte)SequenceElement.SEM, (byte)'1', (byte)'m');
				break;
			case ConsoleColor.DarkGreen:
				SendCSI((byte)'3', (byte)'2', (byte)SequenceElement.SEM, (byte)'1', (byte)'m');
				break;
			case ConsoleColor.DarkYellow:
				SendCSI((byte)'3', (byte)'3', (byte)SequenceElement.SEM, (byte)'1', (byte)'m');
				break;
			case ConsoleColor.DarkBlue:
				SendCSI((byte)'3', (byte)'4', (byte)SequenceElement.SEM, (byte)'1', (byte)'m');
				break;
			case ConsoleColor.DarkMagenta:
				SendCSI((byte)'3', (byte)'5', (byte)SequenceElement.SEM, (byte)'1', (byte)'m');
				break;
			case ConsoleColor.DarkCyan:
				SendCSI((byte)'3', (byte)'6', (byte)SequenceElement.SEM, (byte)'1', (byte)'m');
				break;
			case ConsoleColor.White:
				SendCSI((byte)'3', (byte)'7', (byte)SequenceElement.SEM, (byte)'1', (byte)'m');
				break;
			}
		}

		public Position GetSize()
		{
			HideCursor();
			SaveCursor();
			CursorToColumn(MAX_WIDTH, false);
			CursorDown(MAX_HEIGHT, false);

			var result = GetCursorPosition(true);

			RestoreCursor();
			ShowCursor();
			return result;
		}

		public Position GetCursorPosition(bool useExactValue = false)
		{
			if (useExactValue)
			{
				ControlSequence cs;
				var localBuffer = new List<byte>();
				
				SendCSI();
				SendControlSequence("6n");

				while (true)
				{
					var b = stream.ReadByte(); // can be optimized

					localBuffer.Add((byte)b);

					var validationResult = validator.Check(localBuffer.ToArray(), out cs);
					switch(validationResult)
					{
						case SequenceValidationResult.PrefixFound:
						continue;

						case SequenceValidationResult.SequenceFound:
						if (cs.Type == ControlSequenceType.CursorPosition)
						{
							return cs.Argument as Position;
						}
						Buffer.AddRange(localBuffer);
						localBuffer.Clear();
						continue;

						case SequenceValidationResult.SequenceNotFound:
						Buffer.AddRange(localBuffer);
						localBuffer.Clear();
						continue;
					}
				}
			}
			else
			{
				return vcursor.Position.Clone();
			}
		}

		#endregion

		#region Helper methods

		private void SendControlSequence(params string[] seq)
		{
			foreach(var s in seq)
			{
				foreach(var c in s.ToCharArray())
				{
					stream.WriteByte((byte)c);
				}
			}
		}

		private void SendControlSequence(params byte[] seq)
		{
			foreach(var b in seq)
			{
				stream.WriteByte(b);
			}
		}

		private void SendCSI(params byte[] seq)
		{
			SendControlSequence((byte)SequenceElement.ESC, (byte)SequenceElement.CSI);
			SendControlSequence(seq);
		}

		#endregion

		private enum SequenceElement : byte
		{
			ESC = 0x1B, // <Esc>
 			CSI = 0x5B, // '['
			SEM = 0x3B, // ';'
			INTEGER = 0xFF
		}
	}
}

