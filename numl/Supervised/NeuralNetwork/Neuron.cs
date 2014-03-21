﻿using numl.Math.Functions;
using numl.Math.LinearAlgebra;
using numl.Math.Probability;
using numl.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace numl.Supervised.NeuralNetwork
{
    public class Node
    {
        public Node()
        {
            Output = 0d;
            Input = 0d;
            Delta = 0d;
            Label = String.Empty;
            Out = new List<Edge>();
            In = new List<Edge>();
        }

        public double Output { get; set; }
        public double Input { get; set; }
        public double Delta { get; set; }
        public string Label { get; set; }
        public List<Edge> Out { get; set; }
        public List<Edge> In { get; set; }
        public IFunction Activation { get; set; }

        public void Evaluate()
        {
            // input node?
            if (In.Count == 0)
            {
                Output = Input;
                Out.ForEach(e => e.Target.Evaluate());
            }
            else
            {
                Input = In.Select(e => e.Weight * e.Source.Output).Sum();
                // no output nodes? input passed to output
                if (Out.Count == 0)
                    Output = Input;
                else
                {
                    Output = Activation.Compute(Input);
                    Out.ForEach(e => e.Target.Evaluate());
                }
            }
        }

        public double Error(double t)
        {
            // output node
            if (Out.Count == 0)
                Delta = Output - t;
            else // internal nodes
            {
                double h = Activation == null ? 1 : Activation.Derivative(Output);
                Delta =  h * Out.Select(e => e.Weight * e.Target.Error(t)).Sum();
            }

            return Delta;
        }

        public void Update(double learningRate)
        {
            if (In.Count > 0)
                foreach (Edge edge in In)
                    edge.Weight = learningRate * Delta * Input;

            foreach (Edge edge in Out)
                edge.Target.Update(learningRate);
        }

        public override string ToString()
        {
            return string.Format("{0}", Label);
        }
    }

    public class Edge
    {
        public Edge()
        {
            Weight = Sampling.GetUniform();
        }

        public Node Source { get; set; }
        public Node Target { get; set; }
        public double Weight { get; set; }

        public static Edge Create(Node source, Node target)
        {
            Edge e = new Edge { Source = source, Target = target };
            source.Out.Add(e);
            target.In.Add(e);
            return e;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}] -[{2}]-> [{3}] {4}", Source.Label, Source.Output, Weight, Target.Input, Target.Label);
        }
    }

    public class Network
    {
        public Node[] In { get; set; }
        public Node[] Out { get; set; }

        public static Network Default(Descriptor d, Matrix x, Vector y, IFunction activation)
        {
            Network nn = new Network();
            // set output to number of choices of available
            // 1 if only two choices
            int distinct = y.Distinct().Count();
            int output = distinct > 2 ? distinct : 1;
            
            if (output > 1) throw new NotImplementedException("Still deciding what to do here ;)");

            // set number of hidden units to (Input + Hidden) * 2/3
            // as basic best guess
            int hidden = (int)System.Math.Ceiling((decimal)(x.Cols + output) * 2m / 3m);

            // creating input nodes
            nn.In = new Node[x.Cols + 1];
            nn.In[0] = new Node { Label = "Bias[input]" };
            for (int i = 1; i < x.Cols + 1; i++)
                nn.In[i] = new Node { Label = d.ColumnAt(i - 1) };

            // creating hidden nodes
            Node[] h = new Node[hidden + 1];
            h[0] = new Node { Label = "Bias[hidden]", Input = 1 };
            for (int i = 1; i < hidden + 1; i++)
                h[i] = new Node { Label = "Hidden " + i.ToString(), Activation = activation };

            // creating output nodes
            nn.Out = new Node[output];
            for (int i = 0; i < output; i++)
                nn.Out[i] = new Node { Label = GetLabel(i, d) };

            // link input to hidden. Note: there are
            // no inputs to the hidden bias node
            for (int i = 1; i < h.Length; i++) 
                for (int j = 0; j < nn.In.Length; j++)
                    Edge.Create(nn.In[j], h[i]);

            // link from hidden to output (full)
            for (int i = 0; i < nn.Out.Length; i++)
                for (int j = 0; j < h.Length; j++)
                    Edge.Create(h[j], nn.Out[i]);

            return nn;
        }

        private static string GetLabel(int n, Descriptor d)
        {
            if (d.Label.Type.IsEnum)
                return Enum.GetName(d.Label.Type, n).ToString();
            else if (d.Label is StringProperty && ((StringProperty)d.Label).AsEnum)
                return ((StringProperty)d.Label).Dictionary[n];
            else return d.Label.Name;
        }

        public void Forward(Vector x)
        {
            if (In.Length != x.Length + 1)
                throw new InvalidOperationException("Input nodes not aligned to input vector");

            // set input
            for (int i = 0; i < In.Length; i++)
                In[i].Input = i == 0 ? 1 : x[i - 1];
            // evaluate
            for (int i = 0; i < In.Length; i++)
                In[i].Evaluate();
        }

        public void Back(double t, double learningRate)
        {
            // propagate error gradients
            for (int i = 0; i < In.Length; i++)
                In[i].Error(t);

            // reset weights
            for(int i = 0; i < In.Length; i++)
                In[i].Update(learningRate);
        }
    }
}
