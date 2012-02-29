/*
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
using numl.Math;
using System.Linq;
using numl.Model;

namespace numl.Supervised
{
    public class KNNModel : IModel
    {
        public LabeledDescription Description { get; set; }
        public int K { get; set; }
        public Matrix X { get; set; }
        public Vector Y { get; set; }

        public double Predict(Vector y)
        {
            Tuple<int, double>[] distances = new Tuple<int, double>[y.Length];

            for (int i = 0; i < X.Rows; i++)
            {
                var x = X[i, VectorType.Row];
                distances[i] = new Tuple<int, double>(i, (y - x).Norm());
            }

            var slice = distances
                            .OrderBy(t => t.Item2)
                            .Take(K)
                            .Select(i => i.Item1);

            return Y.Slice(slice).Mode();
        }
    }
}
