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

namespace AntShell.Terminal
{
    public class TerminalMultiplexer
    {
        public BasicTerminalEmulator Current { get; private set; }

        public TerminalMultiplexer(IIOSource world)
        {
            terminals = new List<Tuple<BasicTerminalEmulator, TerminalBuffer>>();

            if (world is IActiveIOSource)
            {
                this.world = (IActiveIOSource) world;
            } 
            else 
            {
                this.world = new PAIOSourceConverter((IPassiveIOSource) world);
            }
        }

        public void AddTerminal(BasicTerminalEmulator terminal)
        {
            terminals.Add(Tuple.Create(terminal, new TerminalBuffer(terminal)));

            if (terminals.Count == 1)
            {
                ChangeTerminal(terminal);
            }
        }

        private void ChangeTerminal(BasicTerminalEmulator caller)
        {
            if (terminals.Count == 1 && caller == null)
            {
                return;
            }

            if (Current != null)
            {
                var prev = Current.InputOutput.Detach() as MITM;
                if (prev != null)
                {
                    prev.Dispose();
                }
            }

            var currentIndex = terminals.FindIndex(x => x.Item1 == Current);
            var next = terminals[(currentIndex + 1) % terminals.Count];
            Current = next.Item1;

            var mitm = new MITM(world, next.Item2);
            mitm.SwitchTerminal += () => ChangeTerminal(null);

            Current.InputOutput.Attach(mitm);
            next.Item2.Replay();
        }

        private readonly IActiveIOSource world;
        private readonly List<Tuple<BasicTerminalEmulator, TerminalBuffer>> terminals;

        private class MITM : IActiveIOSource, IDisposable
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

            #endregion

            public MITM(IActiveIOSource world, TerminalBuffer buffer)
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
