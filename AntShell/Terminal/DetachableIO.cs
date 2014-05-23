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
using AntShell.Encoding;
using System.Threading;
using System.Collections.Generic;

namespace AntShell.Terminal
{
    public class DetachableIO
    {
        public DetachableIO()
        {
            locker = new object();
            localBuffer = new List<byte>();
            encoding = System.Text.Encoding.GetEncoding("UTF-8", System.Text.EncoderFallback.ReplacementFallback, new CustomDecoderFallback());
        }

        public DetachableIO(IIOSource source) : this()
        {
            Attach(source);
        }

        #region Attach/Detach

        public void Attach(IIOSource source)
        {
            lock (locker)
            {
                this.source = source;
                if (_ByteRead != null)
                {
                    AttachActive(_ByteRead);
                }
                else
                {
                    Monitor.Pulse(locker);
                }
            }
        }

        public IIOSource Detach()
        {
            IIOSource result;
            wantToDetach = true;
            lock (locker)
            {
                if (_ByteRead != null)
                {
                    DetachActive(_ByteRead);
                }
                result = source;
                source = null;
            }
            wantToDetach = false;

            if (result is APIOSourceConverter)
            {
                return ((APIOSourceConverter)result).OriginalSource;
            }
            else if (result is PAIOSourceConverter)
            {
                return ((PAIOSourceConverter)result).OriginalSource;
            }
            else
            {
                return result;
            }
        }

        #endregion

        #region Read

        public char? GetNextChar(int timeout = -1)
        {
            return InternalReadCharHandler(GetNextByte, timeout);
        }

        public char? PeekNextChar(int timeout = -1)
        {
            return InternalReadCharHandler(PeekNextByte, timeout);
        }

        public int GetNextByte(int timeout = -1)
        {
            int result;
            if (localBuffer.Count > 0)
            {
                result = localBuffer[0];
                localBuffer.RemoveAt(0);
                return result;
            }

            return InternalRead(timeout);
        }

        public int PeekNextByte(int timeout = -1)
        {
            int result = InternalRead(timeout);
            if (result >= 0)
            {
                localBuffer.Add((byte)result);
            }   
            return result;
        }

        private void EnsurePassiveSource()
        {
            if (source != null && !(source is IPassiveIOSource))
            {
                var converter = source as PAIOSourceConverter;
                source = converter != null ? converter.OriginalSource : new APIOSourceConverter((IActiveIOSource)source);
            }
        }

        private int InternalRead(int timeout)
        {
            if (active)
            {
                throw new InvalidOperationException("Cannot passively read an active IO");
            }

            EnsurePassiveSource();

            var timeoutLeft = timeout;

            while (timeout == -1 || timeoutLeft > 0)
            {
                lock (locker)
                {
                    if (source == null || wantToDetach)
                    {
                        Monitor.Wait(locker);
                        EnsurePassiveSource();
                    }

                    var result = ((IPassiveIOSource)source).Read(500);
                    if (result == -2)
                    {
                        timeoutLeft -= 500;
                        continue;
                    }
                    return result;
                }
            }

            return -2;
        }

        private char? InternalReadCharHandler(Func<int, int> provider, int timeout)
        {
            var bytes = new byte[2];
            int res;

            for (int i = 0; i < 2; i++) 
            {
                res = provider(timeout);
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

        #endregion

        private readonly List<byte> localBuffer;

        public void ClearPeeked()
        {
            localBuffer.Clear();
        }

        public void Flush()
        {
            ClearPeeked();
            source.Flush();
        }

        #region Write

        public void Write(char c)
        {
            foreach (var b in encoding.GetBytes(new [] { c }))
            {
                Write(b);
            }
        }

        public void Write(byte b)
        {
            var lsource = source;
            if (lsource != null)
            {
                lsource.Write(b);
            }

            var bp = BytePrinted;
            if (bp != null)
            {
                bp(b);
            }
        }

        #endregion

        public Action<byte> BytePrinted;

        private event Action<byte> _ByteRead;
        public event Action<byte> ByteRead
        {
            add 
            {
                lock (locker)
                {
                    _ByteRead = value;
                    if (source != null)
                    {
                        AttachActive(value);
                    }
                }
            }

            remove 
            {
                DetachActive(value);
                _ByteRead = null;
            }
        }

        private void AttachActive(Action<byte> value)
        {
            lock (locker)
            {
                if (active)
                {
                    throw new InvalidOperationException("IO can have only one active reader.");
                }

                if (!(source is IActiveIOSource))
                {
                    var converter = source as APIOSourceConverter;
                    source = converter != null ? converter.OriginalSource : new PAIOSourceConverter((IPassiveIOSource)source);
                }

                active = true;
                ((IActiveIOSource)source).ByteRead += value;
            }
        }

        private void DetachActive(Action<byte> value)
        {
            lock (locker)
            {
                if (!active)
                {
                    throw new InvalidOperationException("IO is not in active mode");
                }

                ((IActiveIOSource)source).ByteRead -= value;
                active = false;
            }
        }

        private bool active;

        public IIOSource Source { get { return source; } }
        private IIOSource source;

        private readonly object locker = new object();
        private readonly System.Text.Encoding encoding;

        private bool wantToDetach;
    }
}

