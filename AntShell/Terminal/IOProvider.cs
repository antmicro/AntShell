// *******************************************************************
//
//  Copyright (c) 2013-2016, Antmicro Ltd <antmicro.com>
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
using AntShell.Encoding;

namespace AntShell.Terminal
{
    public class IOProvider : IDisposable
    {
        public IOProvider(IIOSource backend)
        {
            localBuffer = new Queue<byte>();
            encoding = System.Text.Encoding.GetEncoding("UTF-8", System.Text.EncoderFallback.ReplacementFallback, new CustomDecoderFallback());
            this.backend = backend;
            SwitchToPassive();
        }

        public void Dispose()
        {
            var disposable = backend as IDisposable;
            if(disposable != null)
            {
                disposable.Dispose();
            }
        }

        public char? GetNextChar()
        {
            return InternalReadCharHandler(GetNextByte);
        }

        public char? PeekNextChar()
        {
            return InternalReadCharHandler(PeekNextByte);
        }

        private char? InternalReadCharHandler(Func<int> provider)
        {
            var bytes = new byte[4];
            int res;

            for (int i = 0; i < bytes.Length; i++) 
            {
                res = provider();
                if (res < 0)
                {
                    return null;
                }

                bytes[i] = (byte)res;
                var chars = encoding.GetChars(bytes, 0, i + 1)[0];
                if (!((CustomDecoderFallback)encoding.DecoderFallback).IsError) 
                {
                    return chars;
                }
            }

            return null;
        }

        public int GetNextByte()
        {
            if (localBuffer.Count > 0)
            {
                return localBuffer.Dequeue();
            }

            return PassiveReadByte();
        }

        public int PeekNextByte()
        {
            int result = PassiveReadByte();
            if (result >= 0)
            {
                Inject((byte)result);
            }   
            return result;
        }


        public void Write(char c)
        {
            foreach (var b in encoding.GetBytes(new [] { c }))
            {
                Write(b);
            }
        }

        public void Write(byte b)
        {
            var beforeWrite = BeforeWrite;
            var afterWrite = AfterWrite;

            if(beforeWrite != null)
            {
                beforeWrite(b);
            }

            backend.Write(b);

            if (afterWrite != null)
            {
                afterWrite(b);
            }
        }

        public void ClearPeeked()
        {
            localBuffer.Clear();
        }

        public void Flush()
        {
            ClearPeeked();
            backend.Flush();
        }

        public IIOSource Backend { get { return backend; } }

        public event Action<byte> BeforeWrite;
        public event Action<byte> AfterWrite;

        public event Action<int> ByteRead
        {
            add
            {
                SwitchToActive();
                ((IActiveIOSource)backend).ByteRead += value;
            }

            remove
            {
                ((IActiveIOSource)backend).ByteRead -= value;
                if(((IActiveIOSource)backend).IsAnythingAttached)
                {
                    SwitchToPassive();
                }
            }
        }

        internal void Inject(char c)
        {
            foreach(var b in encoding.GetBytes(new [] { c }))
            {
                localBuffer.Enqueue(b);
            }
        }

        private void SwitchToActive()
        {
            var passiveBackend = backend as IPassiveIOSource;
            if(passiveBackend != null)
            {
                backend = new PAIOSourceConverter(passiveBackend);
                isInActiveMode = true;
            }
        }

        private void SwitchToPassive()
        {
            var activeBackend = backend as IActiveIOSource;
            if(activeBackend != null)
            {
                backend = new APIOSourceConverter(activeBackend);
                isInActiveMode = false;
            }
        }

        private int PassiveReadByte()
        {
            if(isInActiveMode)
            {
                throw new InvalidOperationException("One cannot read actively and passively from the same IIOSource at the same time.");
            }
            return ((IPassiveIOSource)backend).Read();
        }

        private IIOSource backend;
        private bool isInActiveMode;

        private readonly Queue<byte> localBuffer;
        private readonly System.Text.Encoding encoding;
    }
}

