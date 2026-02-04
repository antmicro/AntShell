/*
Copyright (c) 2010-2026 Antmicro Ltd <www.antmicro.com>

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

using AntShell.Helpers;

namespace AntShell.Terminal
{
    public class NavigableTerminalEmulator : BasicTerminalEmulator
    {
        private SequenceValidator validator;
        private Queue<char> sequenceQueue;

        public ITerminalHandler Handler { get; set; }

        public ISizeSource SizeSource;

        private VirtualCursor vcursor;

        private bool onceAgain;

        private bool clearScreen = false;

        public NavigableTerminalEmulator(IOProvider io, ISizeSource size) : base(io)
        {
            validator = new SequenceValidator();
            sequenceQueue = new Queue<char>();

            vcursor = new VirtualCursor();
            SizeSource = size;

            ControlSequences();
        }

        public void Start(bool clearScreen)
        {
            this.clearScreen = clearScreen;
            if(clearScreen)
            {
                ClearScreen();
                ResetCursor();
            }
            ResetColors();

            if(SizeSource != null)
            {
                vcursor.TermWidth = SizeSource.Size.X;
                SizeSource.Resized += OnResize;
            }
        }

        private void OnResize()
        {
            if(SizeSource == null) return;

            vcursor.TermWidth = SizeSource.Size.X;

            CursorToStart();
            ClearToEndOfScreen();

            Handler.Redraw();
        }

        private void ControlSequences()
        {
            validator.Add(ControlSequenceType.LeftArrow, (char)SequenceElement.ESC, (char)SequenceElement.CSI, 'D');
            validator.Add(ControlSequenceType.RightArrow, (char)SequenceElement.ESC, (char)SequenceElement.CSI, 'C');
            validator.Add(ControlSequenceType.UpArrow, (char)SequenceElement.ESC, (char)SequenceElement.CSI, 'A');
            validator.Add(ControlSequenceType.DownArrow, (char)SequenceElement.ESC, (char)SequenceElement.CSI, 'B');

            validator.Add(ControlSequenceType.LeftArrow, (char)SequenceElement.ESC, 'O', 'D');
            validator.Add(ControlSequenceType.RightArrow, (char)SequenceElement.ESC, 'O', 'C');
            validator.Add(ControlSequenceType.UpArrow, (char)SequenceElement.ESC, 'O', 'A');
            validator.Add(ControlSequenceType.DownArrow, (char)SequenceElement.ESC, 'O', 'B');

            validator.Add(ControlSequenceType.CtrlLeftArrow, (char)SequenceElement.ESC, (char)SequenceElement.CSI, '1', ';', '5', 'D');
            validator.Add(ControlSequenceType.CtrlRightArrow, (char)SequenceElement.ESC, (char)SequenceElement.CSI, '1', ';', '5', 'C');
            // Alt is sent as an ESC prefix in terminal emulators, and ESC treats lowercase/uppercase differently,
            // which is why we handle both cases separately
            validator.Add(ControlSequenceType.CtrlRightArrow, (char)SequenceElement.ESC, 'f');
            validator.Add(ControlSequenceType.CtrlRightArrow, (char)SequenceElement.ESC, 'F');
            validator.Add(ControlSequenceType.CtrlLeftArrow, (char)SequenceElement.ESC, 'b');
            validator.Add(ControlSequenceType.CtrlLeftArrow, (char)SequenceElement.ESC, 'B');

            validator.Add(ControlSequenceType.AltBackspace, (char)SequenceElement.ESC, (char)0x7f);
            validator.Add(ControlSequenceType.AltD, (char)SequenceElement.ESC, (char)'d');
            validator.Add(ControlSequenceType.AltD, (char)SequenceElement.ESC, (char)'D');

            validator.Add(ControlSequenceType.Delete, (char)SequenceElement.ESC, (char)SequenceElement.CSI, '3', '~');

            validator.Add(ControlSequenceType.Home, (char)SequenceElement.ESC, (char)SequenceElement.CSI, '1', '~');
            validator.Add(ControlSequenceType.Home, (char)SequenceElement.ESC, 'O', 'H');
            validator.Add(ControlSequenceType.Home, (char)SequenceElement.ESC, (char)SequenceElement.CSI, 'H');

            validator.Add(ControlSequenceType.End, (char)SequenceElement.ESC, (char)SequenceElement.CSI, '4', '~');
            validator.Add(ControlSequenceType.End, (char)SequenceElement.ESC, 'O', 'F');
            validator.Add(ControlSequenceType.End, (char)SequenceElement.ESC, (char)SequenceElement.CSI, 'F');

            validator.Add(ControlSequenceType.Esc, (char)SequenceElement.ESC, (char)SequenceElement.ESC);

            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'k'), (char)11);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'r'), (char)18);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 's'), (char)19);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'w'), (char)23);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'c'), (char)3);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'd'), (char)4);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'u'), (char)21);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'a'), (char)1);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'e'), (char)5);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'f'), (char)6);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'b'), (char)2);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'p'), (char)16);
            validator.Add(new ControlSequence(ControlSequenceType.Ctrl, 'n'), (char)14);

            validator.Add(ControlSequenceType.Tab, '\t');
            validator.Add(ControlSequenceType.Backspace, (char)127);
            validator.Add(ControlSequenceType.Backspace, (char)8);
            validator.Add(ControlSequenceType.Enter, '\r');
            validator.Add(ControlSequenceType.Enter, '\n');

            validator.Add(ControlSequenceType.Ignore, (char)12);

            validator.Add((ControlSequenceGenerator)((s, l) =>
            {
                var str = new string(s.ToArray());
                var trimmed = str.Substring(2, str.Length - 3);
                var splitted = trimmed.Split(';');

                return new ControlSequence(ControlSequenceType.CursorPosition, new Position(int.Parse(splitted[1]), int.Parse(splitted[0])));
            }),
                (char)SequenceElement.ESC, (char)SequenceElement.CSI, (char)SequenceElement.INTEGER, ';', (char)SequenceElement.INTEGER, 'R');
        }

        #region Input handling

        public void Stop()
        {
            if(clearScreen)
            {
                ClearScreen();
                ResetCursor();
            }
            ResetColors();

            onceAgain = false;
            if(SizeSource != null)
            {
                SizeSource.Resized -= OnResize;
            }
            InputOutput.CancelGet();
            InputOutput.Dispose();
        }

        public void Run(bool stopOnError = false)
        {
            onceAgain = true;
            while(onceAgain)
            {
                var input = GetNextInput();

                if(input == null)
                {
                    if(stopOnError)
                    {
                        break;
                    }

                    continue;
                }

                HandleInput(input);
            }
        }

        public object GetNextInput()
        {
            while(true)
            {
                var b = InputOutput.GetNextChar();
                if(b == null)
                {
                    return null;
                }

                if(sequenceQueue.Count == 0)
                {
                    if(b == (char)SequenceElement.ESC)
                    {
                        sequenceQueue.Enqueue(b.Value);
                        continue;
                    }

                    // try special control sequence
                    ControlSequence cs;
                    var result = validator.Check(new [] { b.Value }, out cs);
                    if(result == SequenceValidationResult.SequenceFound)
                    {
                        return cs;
                    }

                    // so it must be normal character
                    return b.Value;
                }
                else
                {
                    sequenceQueue.Enqueue(b.Value);
                    ControlSequence cs;
                    var validationResult = validator.Check(sequenceQueue.ToArray(), out cs);
                    if(cs == null)
                    {
                        if(validationResult == SequenceValidationResult.SequenceNotFound)
                        {
                            sequenceQueue.Clear();
                        }
                    }
                    else
                    {
                        sequenceQueue.Clear();
                        return cs;
                    }
                }
            }
        }

        private void HandleInput(object input)
        {
            if(input == null)
            {
                return;
            }

            var inputAsControlSequence = input as ControlSequence;
            if(input is char)
            {
                Handler.HandleCharacter((char)input);
            }
            else if(inputAsControlSequence != null)
            {
                Handler.HandleControlSequence((ControlSequence)input);
            }
        }

        #endregion

        #region Writers

        internal int Write(string text, ConsoleColor? color = null)
        {
            var result = 0;

            if(text != null)
            {
                ColorChangerWrapper(color, () =>
                {
                    foreach(var c in text)
                    {
                        result += WriteChar(c) ? 1 : 0;
                    }
                });
            }

            return result;
        }

        public void Write(char c, ConsoleColor? color = null)
        {
            ColorChangerWrapper(color, () => WriteChar(c));
        }

        public void WriteNoMove(string text, int skip = 0, ConsoleColor? color = null)
        {
            if(text == null) return;

            HideCursor();

            CursorAdvance(skip);
            var count = Write(text, color);
            CursorAdvance(-(count + skip));

            ShowCursor();
        }

        public void WriteRaw(char c, ConsoleColor? color = null)
        {
            ColorChangerWrapper(color, () => InputOutput.Write(c));
        }

        public void WriteRaw(byte[] bs, ConsoleColor? color = null)
        {
            ColorChangerWrapper(color, () =>
            {
                foreach(var b in bs)
                {
                    InputOutput.Write(b);
                }
            }
            );
        }

        public void WriteRaw(string text, ConsoleColor? color = null)
        {
            if(text != null)
            {
                ColorChangerWrapper(color, () =>
                {
                    foreach(var b in text)
                    {
                        InputOutput.Write(b);
                    }
                }
                );
            }
        }

        private void ColorChangerWrapper(ConsoleColor? color, Action action)
        {
            if(color.HasValue)
            {
                SetColor(color.Value);
            }

            action();

            if(color.HasValue)
            {
                ResetColors();
            }
        }

        private bool InEscapeMode = false;

        private bool WriteChar(char c)
        {
            InputOutput.Write(c);

            if(c == (byte)SequenceElement.ESC) // to eliminate control sequences, mostly color change
            {
                InEscapeMode = true;
            }

            if(!InEscapeMode) // if the char changed cursor position by one; check for color steering codes
            {
                if(c == '\r')
                {
                    vcursor.LineFeed();
                }
                else if(c == '\n')
                {
                    vcursor.NewLine();
                }
                else
                {
                    CursorAdvance(1, moveCursor: false);
                }

                return true;
            }
            else
            {
                if(c == 'm')
                {
                    InEscapeMode = false;
                }
            }

            return false;
        }

        #endregion

        #region Cursor movement

        public void CursorUp(int n = 1)
        {
            if(n == 0) return;
            SendCSI($"{n}A");
        }

        public void CursorDown(int n = 1)
        {
            if(n == 0) return;
            SendCSI($"{n}B");
        }

        public void CursorLeft(int n = 1)
        {
            if(n == 0) return;
            SendCSI($"{n}D");
        }

        public void CursorRight(int n = 1)
        {
            if(n == 0) return;
            SendCSI($"{n}C");
        }

        public void CursorMoveBy(Position move)
        {
            if(move.Y > 0)
            {
                CursorDown(move.Y);
            }
            else if(move.Y < 0)
            {
                CursorUp(-move.Y);
            }
            if(move.X > 0)
            {
                CursorRight(move.X);
            }
            else if(move.X < 0)
            {
                CursorLeft(-move.X);
            }
        }

        public void CursorToColumn(int col)
        {
            SendCSI($"{col}G");
        }

        public void CursorAdvance(int n, bool moveCursor = true)
        {
            var move = vcursor.Move(n);
            if(moveCursor)
            {
                CursorMoveBy(move.Delta);
            }
            if(move.NeedNewLine)
            {
                RevealNewLine();
            }
        }

        public void CursorToStart()
        {
            var move = vcursor.MoveToStart();
            CursorMoveBy(move.Delta);
            CursorToColumn(1);
        }

        public void NewLine()
        {
            Write("\n\r");
        }

        // NOTE: This assumes the cursor is at the right edge of the screen.
        //
        // Moves to beginning of the next line without a newline
        public void RevealNewLine()
        {
            SendControlSequence(" \r");
        }

        #endregion

        #region Erase

        public void ClearLineToEndOfScreen()
        {
            SendCSI("2K");
            SendCSI("J");
        }

        public void ClearToEndOfScreen()
        {
            SendCSI("J");
        }

        #endregion

        #region Display

        public void ResetColors()
        {
            if(PlainMode)
            {
                return;
            }
            SendCSI("0m"); // reset colors
        }

        public void SaveCursor()
        {
            SendCSI("s");
        }

        public void RestoreCursor()
        {
            SendCSI("u");
        }

        public void HideCursor()
        {
            SendCSI("?25l");
        }

        public void ShowCursor()
        {
            SendCSI("?25h");
        }

        private static int[] colorSGINumbers;

        static NavigableTerminalEmulator()
        {
            colorSGINumbers = new int[16];
            colorSGINumbers[(int)ConsoleColor.Black] = 30;
            colorSGINumbers[(int)ConsoleColor.DarkRed] = 31;
            colorSGINumbers[(int)ConsoleColor.DarkGreen] = 32;
            colorSGINumbers[(int)ConsoleColor.DarkYellow] = 33;
            colorSGINumbers[(int)ConsoleColor.DarkBlue] = 34;
            colorSGINumbers[(int)ConsoleColor.DarkMagenta] = 35;
            colorSGINumbers[(int)ConsoleColor.DarkCyan] = 36;
            colorSGINumbers[(int)ConsoleColor.Gray] = 37;
            colorSGINumbers[(int)ConsoleColor.DarkGray] = 90;
            colorSGINumbers[(int)ConsoleColor.Red] = 91;
            colorSGINumbers[(int)ConsoleColor.Green] = 92;
            colorSGINumbers[(int)ConsoleColor.Yellow] = 93;
            colorSGINumbers[(int)ConsoleColor.Blue] = 94;
            colorSGINumbers[(int)ConsoleColor.Magenta] = 95;
            colorSGINumbers[(int)ConsoleColor.Cyan] = 96;
            colorSGINumbers[(int)ConsoleColor.White] = 97;
        }

        public void SetColor(ConsoleColor color)
        {
            if(PlainMode)
            {
                return;
            }

            int SGRNumber = colorSGINumbers[(int)color];
            SendCSI($"{SGRNumber}m");
        }

        #endregion
    }
}

