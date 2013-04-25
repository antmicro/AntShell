/*
Copyright (c) 2013 Ant Micro <www.antmicro.com>

Authors:
* Mateusz Holenko (mholenko@antmicro.com)

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
using System.Text;

namespace AntShell.Helpers
{
	public static class Helper
	{
		public static string CommonPrefix(IEnumerable<string> items)
		{
			StringBuilder commonPrefix = null;

			foreach(var item in items)
			{
				if (item == null)
				{
					continue;
				}

				if (commonPrefix == null)
				{
					commonPrefix = new StringBuilder(item);
				}
				else
				{
					var index = commonPrefix.ToString().Zip(item, (c1, c2) => c1 == c2).TakeWhile(b => b).Count();
					commonPrefix.Remove(index, commonPrefix.Length - index);
				}
			}

			return (commonPrefix != null) ? commonPrefix.ToString() : null;
		}
	}
}

