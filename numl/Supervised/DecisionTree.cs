﻿using System;
using numl.Math;
using numl.Model;
using System.Linq;
using numl.Math.Information;
using numl.Math.LinearAlgebra;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text;

namespace numl.Supervised
{
    public class DecisionTreeGenerator : Generator
    {
        public int Depth { get; set; }
        public int Width { get; set; }
        public double Hint { get; set; }
        public Type ImpurityType { get; set; }

        private Impurity _impurity;
        public Impurity Impurity
        {
            get
            {
                if (_impurity == null)
                    _impurity = (Impurity)Activator.CreateInstance(ImpurityType);
                return _impurity;
            }
        }

        public DecisionTreeGenerator(
            int depth = 5,
            int width = 2,
            Descriptor descriptor = null,
            Type impurityType = null,
            double hint = double.Epsilon)
        {
            if (width < 2)
                throw new InvalidOperationException("Cannot set dt tree width to less than 2!");

            Descriptor = descriptor;
            Depth = depth;
            Width = width;
            ImpurityType = impurityType ?? typeof(Entropy);
            Hint = hint;
        }

        public override IModel Generate(Matrix x, Vector y)
        {
            if (Descriptor == null)
                throw new InvalidOperationException("Cannot build decision tree without type knowledge!");

            var n = BuildTree(x, y, Depth, new List<int>(x.Cols));

            return new DecisionTreeModel
            {
                Descriptor = Descriptor,
                Tree = n,
                Hint = Hint
            };
        }

        private Node BuildTree(Matrix x, Vector y, int depth, List<int> used)
        {
            if (depth < 0 || y.Distinct().Count() == 1)
                return BuildLeafNode(y.Mode());

            var tuple = GetBestSplit(x, y, used);
            var col = tuple.Item1;
            var gain = tuple.Item2;
            var measure = tuple.Item3;



            // uh oh, need to return something?
            // a weird node of some sort...
            // but just in case...
            if (col == -1)
                return BuildLeafNode(y.Mode());

#if DEBUG
            Console.WriteLine("Depth: {0}, Best Split: [{1}], Gain ({3})", depth, Descriptor.ColumnAt(col), col, gain);
#endif

            used.Add(col);

            Node node = new Node
            {
                Column = col,
                Gain = gain,
                IsLeaf = false,
                Name = Descriptor.ColumnAt(col),
                Edges = new Edge[measure.Segments.Length]
            };

            // populate edges
            for (int i = 0; i < node.Edges.Length; i++)
            {
                // working set
                var segment = measure.Segments[i];
                node.Edges[i] = new Edge();
                var edge = node.Edges[i];

                edge.Parent = node;
                edge.Discrete = measure.Discrete;
                edge.Min = segment.Min;
                edge.Max = segment.Max;

                IEnumerable<int> slice;

                if (edge.Discrete)
                {
                    // get discrete label
                    edge.Label = Descriptor.At(col).Convert(segment.Min).ToString();
                    // do value check for matrix slicing
                    slice = x.Indices(v => v[col] == segment.Min);
                }
                else
                {
                    // get range label
                    edge.Label = string.Format("{0} ≤ x < {1}", segment.Min, segment.Max);
                    // do range check for matrix slicing
                    slice = x.Indices(v => v[col] >= segment.Min && v[col] < segment.Max);
                }

#if DEBUG
                Console.WriteLine("\tBuilding Child for {0}", edge.Label);
#endif
                edge.Child = BuildTree(x.Slice(slice), y.Slice(slice), depth - 1, used);
            }

#if DEBUG
            Console.WriteLine("------------------------------------------------------------");
#endif

            return node;
        }

        private Tuple<int, double, Impurity> GetBestSplit(Matrix x, Vector y, List<int> used)
        {
            double bestGain = -1;
            int bestFeature = -1;

            Impurity bestMeasure = null;
            for (int i = 0; i < x.Cols; i++)
            {
                // already used?
                if (used.Contains(i)) continue;

                double gain = 0;
                Impurity measure = (Impurity)Activator.CreateInstance(ImpurityType);
                // get appropriate column vector
                var feature = x.Col(i);
                // get appropriate feature at index i
                // (important on because of multivalued
                // cols)
                var property = Descriptor.At(i);
                // if discrete, calculate full relative gain
                if (property.Discrete)
                    gain = measure.Gain(y, feature);
                // otherwise segment based on width
                else
                    gain = measure.SegmentedGain(y, feature, Width);

#if DEBUG
                Console.WriteLine("\t\tGain for {0} = {1:0.0000}", Descriptor.ColumnAt(i), gain);
#endif

                // best one?
                if (gain > bestGain)
                {
                    bestGain = gain;
                    bestFeature = i;
                    bestMeasure = measure;
                }
            }

            return new Tuple<int, double, Impurity>(bestFeature, bestGain, bestMeasure);
        }

        private Node BuildLeafNode(double val)
        {
#if DEBUG
            Console.WriteLine("Building leaf node: {0}", Descriptor.Label.Convert(val));
#endif
            // build leaf node
            return new Node { IsLeaf = true, Value = val, Edges = new Edge[] { }, Label = Descriptor.Label.Convert(val) };
        }
    }

    [XmlRoot("dt")]
    public class DecisionTreeModel : Model
    {
        [XmlElement("tree")]
        public Node Tree { get; set; }
        [XmlAttribute("hint")]
        public double Hint { get; set; }

        public DecisionTreeModel()
        {
            // no hint
            Hint = double.Epsilon;
        }

        public override double Predict(Vector y)
        {
            return WalkNode(y, Tree);
        }

        private double WalkNode(Vector v, Node node)
        {
            if (node.IsLeaf)
                return node.Value;

            // Get the index of the feature for this node.
            var col = node.Column;
            if (col == -1)
                throw new InvalidOperationException("Invalid Feature encountered during node walk!");

            for (int i = 0; i < node.Edges.Length; i++)
            {
                Edge edge = node.Edges[i];
                if (edge.Discrete && v[col] == edge.Min)
                    return WalkNode(v, edge.Child);
                if (!edge.Discrete && v[col] >= edge.Min && v[col] < edge.Min)
                    return WalkNode(v, edge.Child);
            }

            if (Hint != double.Epsilon)
                return Hint;
            else
                throw new InvalidOperationException(String.Format("Unable to match split value {0} for feature {1}[2]", v[col], Descriptor.At(col), col));
        }

        public override string ToString()
        {
            return PrintNode(Tree, "\t");   
        }

        private string PrintNode(Node n, string pre)
        {
            if (n.IsLeaf)
                return String.Format("{0} +({1}, {2:#.####})\n", pre, n.Label, n.Value);
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(String.Format("{0}[{1}, {2:0.0000}]", pre, n.Name, n.Gain));
                foreach (Edge edge in n.Edges)
                {
                    sb.AppendLine(String.Format("{0} |- {1}", pre, edge.Label));
                    sb.Append(PrintNode(edge.Child, String.Format("{0} |\t", pre)));
                }

                return sb.ToString();
            }
        }
    }
}
