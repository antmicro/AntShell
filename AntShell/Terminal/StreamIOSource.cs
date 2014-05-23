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
using System.IO;

namespace AntShell.Terminal
{
    public class StreamIOSource : IPassiveIOSource
    {
        #region ITerminalIOSource implementation

        public void Flush()
        {
            InputStream.Flush();
        }

        public void Write(byte b)
        {
            OutputStream.WriteByte(b);
        }

        public int Read(int timeout)
        {
            int result;

            if (timeout < 0 || !InputStream.CanTimeout)
            {
                return InputStream.ReadByte();
            }

            var current = InputStream.ReadTimeout;
            InputStream.ReadTimeout = timeout;
            result = InputStream.ReadByte();
            InputStream.ReadTimeout = current;
            return result;
        }

        public void Close()
        {
            InputStream.Close();
            OutputStream.Close();
        }

        #endregion

        public string Name { get; private set; }

        public StreamIOSource(Stream stream, string name = null) : this(stream, stream, name)
        {
        }

        public StreamIOSource(Stream input, Stream output, string name = null)
        {
            InputStream = input;
            OutputStream = output;

            Name = name;
        }

        public Stream InputStream { get; private set; }
        public Stream OutputStream { get; private set; }
    }
}

