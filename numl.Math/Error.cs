﻿/*
 Copyright (c) 2012 Seth Juarez

 Permission is hereby granted, free of charge, to any person obtaining a copy
 of this software and associated documentation files (the "Software"), to deal
 in the Software without restriction, including without limitation the rights
 to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 copies of the Software, and to permit persons to whom the Software is
 furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in
 all copies or substantial portions of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace numl.Math
{
    public class Error : Impurity
    {
        internal Error()
        {
            _conditional = false;
            _width = Int16.MaxValue;
        }

        public Error(Vector x, Vector y = null, int width = 2)
        {
            _x = x;
            if (y != null)
                _y = y;
            _width = width;
            _conditional = false;
        }

        internal override double Calculate(Vector x)
        {
            if (x == null)
                throw new InvalidOperationException("x does not exist!");

            var px = (from i in x.Distinct()
                      let q = (from j in x
                               where j == i
                               select j).Count()
                      select (q / (double)x.Length)).Max();

            return System.Math.Round(1 - px, 4);
        }

        public static Error Of(Vector x)
        {
            return new Error { _x = x, _conditional = false };
        }
    }
}
