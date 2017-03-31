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
using System.Collections.Concurrent;
using System.Threading;
using System;

namespace AntShell.Terminal
{
    public class APIOSourceConverter : IPassiveIOSource
    {
        public void Dispose()
        {
        }

        #region IPassiveIOSource implementation

        public int Read()
        {
            if(isDone)
            {
                return -1;
            }

            int result;
            try
            {
                result = buffer.Take(readCancelationTokenSource.Token);
            }
            catch(OperationCanceledException)
            {
                result = -1;
            }

            if(result == -1)
            {
                isDone = true;
            }
            return result;
        }

        public void CancelRead()
        {
            readCancelationTokenSource.Cancel();
        }

        #endregion

        #region IIOSource implementation

        public void Flush()
        {
            activeSource.Flush();
        }

        public void Write(byte b)
        {
            activeSource.Write(b);
        }

        #endregion

        public APIOSourceConverter(IActiveIOSource source)
        {
            readCancelationTokenSource = new CancellationTokenSource();
            activeSource = source;
            buffer = new BlockingCollection<int>();

            activeSource.ByteRead += buffer.Add;
        }

        public IActiveIOSource OriginalSource { get { return activeSource; } }

        private bool isDone;
        private CancellationTokenSource readCancelationTokenSource;

        private readonly IActiveIOSource activeSource;
        private readonly BlockingCollection<int> buffer;
    }
}

