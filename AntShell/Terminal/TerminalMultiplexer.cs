// *******************************************************************
//
//  Copyright (c) 2013-2014, Antmicro Ltd <antmicro.com>
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// *******************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShell.Terminal
{
    public class TerminalMultiplexer
    {
        public BasicTerminalEmulator Current { get; private set; }

        public TerminalMultiplexer(IIOSource world)
        {
            terminals = new List<MulitplexerItem>();

            if (world is IActiveIOSource)
            {
                this.world = (IActiveIOSource) world;
            } 
            else 
            {
                this.world = new PAIOSourceConverter((IPassiveIOSource) world);
            }
        }

        public List<string> AvailableTerminals()
        {
            return terminals.Select(x => x.Name).ToList();
        }

        public void AddTerminal(string name, BasicTerminalEmulator terminal)
        {
            terminals.Add(new MulitplexerItem { Terminal = terminal, Buffer = new TerminalBuffer(terminal), Name = name });

            if (terminals.Count == 1)
            {
                ChangeTerminal(terminal);
            }
        }

        public void ChangeTerminalTo(string name)
        {
            if (terminals.Count == 1)
            {
                return;
            }

            var next = terminals.First(x => x.Name == name);
            InnerChangeTerminal(next);
        }

        private void ChangeTerminal(BasicTerminalEmulator caller)
        {
            if (terminals.Count == 1 && caller == null)
            {
                return;
            }

            var currentIndex = terminals.FindIndex(x => x.Terminal == Current);
            var next = terminals[(currentIndex + 1) % terminals.Count];
            InnerChangeTerminal(next);
        }

        private void InnerChangeTerminal(MulitplexerItem next)
        {
            if (Current != null)
            {
                var prev = Current.InputOutput.Detach() as IOInterceptor;
                if (prev != null)
                {
                    prev.Dispose();
                }
            }

            Current = next.Terminal;

            var mitm = new IOInterceptor(world, next.Buffer);
            mitm.SwitchTerminal += () => ChangeTerminal(null);

            Current.InputOutput.Attach(mitm);
            next.Buffer.Replay();
        }

        private readonly IActiveIOSource world;
        private readonly List<MulitplexerItem> terminals;

        private struct MulitplexerItem
        {
            public BasicTerminalEmulator Terminal;
            public TerminalBuffer Buffer;
            public string Name;
        }

        private class IOInterceptor : IActiveIOSource, IDisposable
        {
            #region IDisposable implementation

            public void Dispose()
            {
                world.ByteRead -= HandleByteRead;
                ByteRead = null;
            }

            #endregion

            public event Action SwitchTerminal;

            public event Action<byte> ByteRead;

            #region IIOSource implementation

            public void Flush()
            {
                world.Flush();
            }

            public void Write(byte b)
            {
                world.Write(b);
            }

            public string Name { get { return "IOInterceptor"; } }

            #endregion

            public IOInterceptor(IActiveIOSource world, TerminalBuffer buffer)
            {
                this.world = world;
                this.buffer = buffer;
                world.ByteRead += HandleByteRead;
            }

            private void HandleByteRead (byte b)
            {
                if (b == 30)
                {
                    var st = SwitchTerminal;
                    if (st != null)
                    {
                        st();
                        return;
                    }
                }

                var br = ByteRead;
                if (br != null)
                {
                    br(b);
                }
            }

            private readonly IActiveIOSource world;
            private readonly TerminalBuffer buffer;
        }
    }
}
