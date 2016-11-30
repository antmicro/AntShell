﻿// *******************************************************************
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

        private char? InternalReadCharHandler(Func<int> provider)
        {
            while(true)
            {
                var firstByte = provider();
                var numberOfBytesToRead = 0;
                if(firstByte < 0)
                {
                    return null;
                }
                if(firstByte < 0x80)
                {
                    return (char)firstByte;
                }
                else if((firstByte & 0xE0) == 0xC0)
                {
                    // two bytes
                    numberOfBytesToRead = 1;
                }
                else if((firstByte & 0xF0) == 0xE0)
                {
                    // three bytes
                    numberOfBytesToRead = 2;
                }
                else
                {
                    // four bytes
                    numberOfBytesToRead = 3;
                }

                var bytes = new byte[numberOfBytesToRead + 1];
                bytes[0] = (byte)firstByte;
                for(int i = 1; i < bytes.Length; i++)
                {
                    var nextByte = provider();
                    if(nextByte < 0)
                    {
                        return null;
                    }
                    bytes[i] = (byte)nextByte;
                }

                var decodedChar = encoding.GetChars(bytes)[0];
                if(!((CustomDecoderFallback)encoding.DecoderFallback).IsError)
                {
                    return decodedChar;
                }
            }
        }

        public int GetNextByte()
        {
            if(isInActiveMode)
            {
                throw new InvalidOperationException("Cannot use 'GetNextByte' method when a callback is attached to 'ByteRead' event.");
            }

            if(localBuffer.Count > 0)
            {
                return localBuffer.Dequeue();
            }

            return ((IPassiveIOSource)backend).Read();
        }


        public void Write(char c)
        {
            foreach(var b in encoding.GetBytes(new [] { c }))
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

            if(afterWrite != null)
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
            if(isInActiveMode)
            {
                throw new InvalidOperationException("Cannot use 'Inject' method when a callback is attached to 'ByteRead' event.");
            }

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

        private IIOSource backend;
        private bool isInActiveMode;

        private readonly Queue<byte> localBuffer;
        private readonly System.Text.Encoding encoding;
    }
}

