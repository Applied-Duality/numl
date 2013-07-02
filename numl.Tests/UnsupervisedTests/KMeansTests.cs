﻿using numl.Math.LinearAlgebra;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using numl.Math.Probability;
using numl.Unsupervised;
using numl.Math.Metrics;
using numl.Model;

namespace numl.Tests.UnsupervisedTests
{
    public class AB
    {
        [Feature]
        public double A { get; set; }
        [Feature]
        public double B { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", A, B);
        }
    }

    [TestFixture]
    public class KMeansTests
    {
        private static Matrix GenerateData(int size)
        {
            var A = Matrix.Create(size, 2, () => Sampling.GetNormal());
            A[0, VectorType.Col] -= 20;
            A[1, VectorType.Col] -= 20;

            var B = Matrix.Create(size, 2, () => Sampling.GetNormal());
            B[0, VectorType.Col] += 20;
            B[1, VectorType.Col] += 20;

            var X = A.Stack(B);
            return X;
        }

        [TestCase(10)]
        [TestCase(20)]
        [TestCase(50)]
        [TestCase(100)]
        public void Test_Numerical_KMeans(int size)
        {
            Matrix X = GenerateData(size);

            KMeans model = new KMeans();
            var assignment = model.Generate(X, 2, new EuclidianDistance());
            Assert.AreEqual(size * 2, assignment.Length);
            var a1 = assignment.First();
            var a2 = assignment.Last();
            for (int i = 0; i < size * 2; i++)
            {
                if (i < size)
                    Assert.AreEqual(a1, assignment[i]);
                else
                    Assert.AreEqual(a2, assignment[i]);
            }
        }

        [TestCase(10)]
        [TestCase(20)]
        [TestCase(50)]
        [TestCase(100)]
        public void Test_Object_KMeans(int size)
        {
            Matrix X = GenerateData(size);
            var objects = X.GetRows()
                           .Select(v => new AB { A = v[0], B = v[1] })
                           .ToArray();

            var descriptor = Descriptor.Create<AB>();

            KMeans model = new KMeans();
            var clusters = model.Generate(descriptor, objects, 2, new EuclidianDistance());
            Assert.AreEqual(2, clusters.Children.Length);
        }

    }
}
