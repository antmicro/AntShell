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
using AntShell.Helpers;

namespace AntShell.Terminal
{
    internal class VirtualCursor
    {
        public VirtualCursor()
        {
            Position = new Position(1, 1);
            TermWidth = 0;
        }

        /// 1-based and relative to the beginning of the prompt line.
        public Position Position;

        /// Amount of characters since the beginning of the prompt line.
        public int CharPosition
        {
            get => (Position.X - 1) + (Position.Y - 1) * TermWidth;
            set => Position = TermWidth == 0 ? new Position(value + 1, 1) : new Position(value % TermWidth + 1, value / TermWidth + 1);
        }

        private int termWidth;
        public int TermWidth
        {
            get => termWidth;
            set
            {
                var charPos = CharPosition;
                termWidth = value;
                CharPosition = charPos;
            }
        }

        /// Makes a new non-wrapping line
        public void NewLine()
        {
            Position.Y = 1;
        }

        public void LineFeed()
        {
            Position.X = 1;
        }

        public MoveResult MoveToStart() => Move(-CharPosition);

        public MoveResult Move(int n)
        {
            var oldPos = Position;
            CharPosition += n;
            var newPos = Position;
            var needNewline = n > 0 && Position.X == 1;
            if(needNewline)
            {
                newPos.X = TermWidth;
            }
            return new MoveResult(newPos - oldPos, needNewline);
        }

        public struct MoveResult
        {
            public MoveResult(Position delta, bool needNewline = false)
            {
                Delta = delta;
                NeedNewLine = needNewline;
            }
            public Position Delta;
            public bool NeedNewLine;
        }
    }
}

