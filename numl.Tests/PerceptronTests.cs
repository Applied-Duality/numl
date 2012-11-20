﻿using MathNet.Numerics.LinearAlgebra.Double;
using numl.Supervised;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace numl.Tests
{
    [TestFixture]
    public class PerceptronTests
    {
        [Test]
        public void Test_Perceptron_Simple()
        {
            PerceptronGenerator generator = new PerceptronGenerator();
            DenseMatrix x = new DenseMatrix(new double[,] {
                { 1, 0, 0 },
                { 1, 0, 1 },
                { 1, 1, 0 },
                { 1, 1, 1 }
            });

            var test = x.Clone();

            DenseVector y = new DenseVector(new double[] { 1, 1, -1, -1 });

            var model = generator.Generate(x, y);

            DenseVector z = new DenseVector(4);

            for (int i = 0; i < test.RowCount; i++)
                z[i] = model.Predict((Vector)test.Row(i)) <= 0 ? -1 : 1;

            Assert.AreEqual(y, z);
        }

        [Test]
        public void Test_Perceptron_Simple_2()
        {
            PerceptronGenerator generator = new PerceptronGenerator();
            DenseMatrix x = new DenseMatrix(new double[,] {
                { 1, 4 }, // yes
                { -1, 3 }, // no
                { -1, 2 }, // no
                { -1, 1 }, // no
                { -2, 1 }, // no
                { -2, 2 }, // no
                { 2, 3 }, // yes
                { 3, 2 }, // yes
                { 3, 3 }, // yes
                { 4, 2 }, // yes
                { 4, 1 }, // yes
            });

            var test = x.Clone();

            DenseVector y = new DenseVector(new double[] { 1, -1, -1, -1, -1, -1, 1, 1, 1, 1, 1 });

            var model = generator.Generate(x, y);

            DenseVector z = new DenseVector(11);

            for (int i = 0; i < test.RowCount; i++)
                z[i] = model.Predict((Vector)test.Row(i)) <= 0 ? -1 : 1;

            Assert.AreEqual(y, z);
        }
    }
}
