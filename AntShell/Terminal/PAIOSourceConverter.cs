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
using System.Threading;
using Antmicro.Migrant.Hooks;

namespace AntShell.Terminal
{
    public class PAIOSourceConverter : IActiveIOSource
    {
        #region IActiveIOSource implementation

        public event Action<byte> ByteRead;

        #endregion

        #region IIOSource implementation

        public void Flush()
        {
            passiveSource.Flush();
        }

        public void Write(byte b)
        {
            passiveSource.Write(b);
        }

        public string Name { get { return OriginalSource.Name; } }

        #endregion

        public PAIOSourceConverter(IPassiveIOSource source)
        {
            passiveSource = source;
            Init();
        }

        public IPassiveIOSource OriginalSource { get { return passiveSource; } }

        [PostDeserialization]
        private void Init()
        {
            var reader = new Reader(this);
            reader.Run();
        }

        private readonly IPassiveIOSource passiveSource;

        private class Reader
        {
            public Reader(PAIOSourceConverter c)
            {
                converter = c;
            }

            public void Run()
            {
                thread = new Thread(Loop) {
                    Name = "PAIOSourceConverter runner",
                    IsBackground = true
                };
                thread.Start();
            }

            public void Halt()
            {
                thread.Join();
            }

            private void Loop()
            {
                while(true)
                {
                    var read = converter.passiveSource.Read(-1);
                    if (read == -1)
                    {
                        break;
                    }
                 
                    var byteRead = converter.ByteRead;
                    if (byteRead != null)
                    {
                        byteRead((byte)read);
                    }
                }
            }

            private readonly PAIOSourceConverter converter;

            private Thread thread;
        }
    }
}

