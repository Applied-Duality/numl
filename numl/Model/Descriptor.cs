﻿using System;
using numl.Utils;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using numl.Math.LinearAlgebra;
using System.Text;

namespace numl.Model
{
    /// <summary>
    /// Theis class is designed to describe the underlying types that
    /// will be used in the machine learning process. Any machine learning
    /// process requires a set of <see cref="Features"/> that will be used to discriminate
    /// the <see cref="Label"/>. The <see cref="Label"/> itself is the target element that the machine
    /// learning algorithms learn to predict.
    /// </summary>
    public class Descriptor
    {
        /// <summary>
        /// Set of features used to discriminate or 
        /// learn about the <see cref="Label"/>.
        /// </summary>
        public Property[] Features { get; set; }
        /// <summary>
        /// Target property that is the target
        /// of machine learning.
        /// </summary>
        public Property Label { get; set; }

        /// <summary>
        /// Available column names used to discriminate
        /// or learn about <see cref="Label"/>. The number
        /// of columns does not necessarily equal the
        /// number of <see cref="Features"/> given that
        /// there might exist multi-valued features.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetColumns()
        {
            foreach (var p in Features)
                foreach (var s in p.GetColumns())
                    yield return s;
        }

        private int _vectorLength = -1;
        /// <summary>
        /// Total feature count The number
        /// of features does not necessarily equal the
        /// number of <see cref="Features"/> given that
        /// there might exist multi-valued features.
        /// </summary>
        public int VectorLength
        {
            get
            {
                if (_vectorLength < 0)
                    _vectorLength = Features.Select(f => f.Length).Sum();

                return _vectorLength;
            }
        }

        /// <summary>
        /// Base type of object being described. 
        /// This could also be null.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Gets related property given its
        /// offset within the vector representation
        /// </summary>
        /// <param name="i">Vector Index</param>
        /// <returns>Associated Feature</returns>
        public Property At(int i)
        {
            if (i < 0 || i > VectorLength)
                throw new IndexOutOfRangeException(string.Format("{0} falls outside of the appropriate range", i));

            var q = (from p in Features
                     where i >= p.Start && i < p.Start + p.Length
                     select p);

            return q.First();
        }

        /// <summary>
        /// Gets related property column name given its
        /// offset within the vector representation
        /// </summary>
        /// <param name="i">Vector Index</param>
        /// <returns>Associated Property Name</returns>
        public string ColumnAt(int i)
        {
            var prop = At(i);
            var offset = i - prop.Start;
            var col = prop.GetColumns().ElementAt(offset);
            return col;
        }

        /// <summary>
        /// Converts a given example into a lazy
        /// list of doubles in preparation for
        /// vector conversion (both features
        /// and corresponding label)
        /// </summary>
        /// <param name="item">Example</param>
        /// <returns>Lazy List of doubles</returns>
        public IEnumerable<double> Convert(object item)
        {
            return Convert(item, true);
        }

        /// <summary>
        /// Converts a given example into a lazy
        /// list of doubles in preparation for
        /// vector conversion
        /// </summary>
        /// <param name="item">Example</param>
        /// <param name="withLabel">Should convert label as well</param>
        /// <returns>Lazy List of doubles</returns>
        public IEnumerable<double> Convert(object item, bool withLabel)
        {
            if (Features.Length == 0)
                throw new InvalidOperationException("Cannot convert item with an empty Feature set.");

            for (int i = 0; i < Features.Length; i++)
            {
                // current feature
                var feature = Features[i];

                // pre-process item
                feature.PreProcess(item);

                // start start position
                if (feature.Start < 0)
                    feature.Start = i == 0 ? 0 : Features[i - 1].Start + Features[i - 1].Length;

                // retrieve item
                var o = FastReflection.Get(item, feature.Name);

                // convert item
                foreach (double val in feature.Convert(o))
                    yield return val;

                // post-process item
                feature.PostProcess(item);
            }

            // convert label (if available)
            if (Label != null && withLabel)
                foreach (double val in Label.Convert(FastReflection.Get(item, Label.Name)))
                    yield return val;

        }

        /// <summary>
        /// Converts a list of examples into a lazy double
        /// list of doubles
        /// </summary>
        /// <param name="items">Examples</param>
        /// <returns>Lazy double enumerable of doubles</returns>
        public IEnumerable<IEnumerable<double>> Convert(IEnumerable<object> items)
        {
            // Pre processing items
            foreach (Property feature in Features)
                feature.PreProcess(items);

            if (Label != null)
                Label.PreProcess(items);

            // convert items
            foreach (object o in items)
                yield return Convert(o);

            // Post processing items
            foreach (Property feature in Features)
                feature.PostProcess(items);

            if (Label != null)
                Label.PostProcess(items);
        }

        /// <summary>
        /// Converts a list of examples into a Matrix/Vector tuple
        /// </summary>
        /// <param name="examples">Examples</param>
        /// <returns>Tuple containing Matrix and Vector</returns>
        public Tuple<Matrix, Vector> ToExamples(IEnumerable<object> examples)
        {
            return Convert(examples).ToExamples();
        }

        /// <summary>
        /// Converts a list of examples into a Matrix
        /// </summary>
        /// <param name="examples">Examples</param>
        /// <returns>Matrix representation</returns>
        public Matrix ToMatrix(IEnumerable<object> examples)
        {
            return Convert(examples).ToMatrix();
        }

        /// <summary>
        /// Pretty printed descriptor
        /// </summary>
        /// <returns>Pretty printed string</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(String.Format("Descriptor ({0}) {{", Type == null ? "N/A" : Type.Name));
            for (int i = 0; i < Features.Length; i++)
                sb.AppendLine(string.Format("   {0}", Features[i]));
            if (Label != null)
                sb.AppendLine(string.Format("  *{0}", Label));

            sb.AppendLine("}");
            return sb.ToString();
        }

        //---- Creational

        /// <summary>
        /// Creates a descriptor based upon a marked up
        /// concrete class
        /// </summary>
        /// <typeparam name="T">Class Type</typeparam>
        /// <returns>Descriptor</returns>
        public static Descriptor Create<T>()
            where T : class
        {
            return Create(typeof(T));
        }

        /// <summary>
        /// Creates a descriptor based upon a marked up
        /// concrete type
        /// </summary>
        /// <param name="t">Class Type</param>
        /// <returns>Descriptor</returns>
        public static Descriptor Create(Type t)
        {

            if (!t.IsClass)
                throw new InvalidOperationException("Can only work with class types");

            List<Property> features = new List<Property>();
            Property label = null;

            foreach (PropertyInfo property in t.GetProperties())
            {
                var item = property.GetCustomAttributes(typeof(NumlAttribute), false);

                if (item.Length == 1)
                {
                    var attrib = (NumlAttribute)item[0];

                    // generate appropriate property from attribute
                    Property p = attrib.GenerateProperty(property);

                    // feature
                    if (attrib.GetType().IsSubclassOf(typeof(FeatureAttribute)) ||
                        attrib is FeatureAttribute)
                        features.Add(p);
                    // label
                    else if (attrib.GetType().IsSubclassOf(typeof(LabelAttribute)) ||
                        attrib is LabelAttribute)
                    {
                        if (label != null)
                            throw new InvalidOperationException("Cannot have multiple labels in a class");
                        label = p;
                    }
                }
            }

            return new Descriptor
            {
                Features = features.ToArray(),
                Label = label,
                Type = t
            };
        }

        /// <summary>
        /// Creates a new descriptor using
        /// a fluent approach. This initial
        /// descriptor is worthless without 
        /// adding features
        /// </summary>
        /// <returns>Empty Descriptor</returns>
        public static Descriptor New()
        {
            return new Descriptor() { Features = new Property[] { } };
        }

        /// <summary>
        /// Adds a new feature to descriptor
        /// </summary>
        /// <param name="name">Name of feature (must match property name or dictionary key)</param>
        /// <returns>method for describing feature</returns>
        public DescriptorProperty With(string name)
        {
            return new DescriptorProperty(this, name, false);
        }

        /// <summary>
        /// Adds (or replaces) a label to the descriptor
        /// </summary>
        /// <param name="name">Name of label (must match property name or dictionary key)</param>
        /// <returns></returns>
        public DescriptorProperty Learn(string name)
        {
            return new DescriptorProperty(this, name, true);
        }

    }


    /// <summary>
    /// Fluent API addition for simplifying the process
    /// of adding features and labels to a descriptor
    /// </summary>
    public class DescriptorProperty
    {
        private readonly Descriptor _descriptor;
        private readonly string _name;
        private readonly bool _label;

        /// <summary>
        /// internal constructor used for
        /// creating chaining
        /// </summary>
        /// <param name="descriptor">descriptor</param>
        /// <param name="name">name of property</param>
        /// <param name="label">label property?</param>
        internal DescriptorProperty(Descriptor descriptor, string name, bool label)
        {
            _label = label;
            _name = name;
            _descriptor = descriptor;
        }

        /// <summary>
        /// Not ready
        /// </summary>
        /// <param name="conversion">Conversion method</param>
        /// <returns>Descriptor</returns>
        public Descriptor Use(Func<object, double> conversion)
        {
            throw new NotImplementedException("Not yet ;)");
            //return _descriptor;
        }

        /// <summary>
        /// Adds property to descriptor
        /// with chained name and type
        /// </summary>
        /// <param name="type">Property Type</param>
        /// <returns>descriptor with added property</returns>
        public Descriptor As(Type type)
        {
            Property p;
            if (_label)
                p = TypeHelpers.GenerateLabel(type, _name);
            else
                p = TypeHelpers.GenerateFeature(type, _name);
            AddProperty(p);
            return _descriptor;
        }

        /// <summary>
        /// Adds the default string property to 
        /// descriptor with previously chained name 
        /// </summary>
        /// <returns>descriptor with added property</returns>
        public Descriptor AsString()
        {
            StringProperty p = new StringProperty();
            p.AsEnum = _label;
            AddProperty(p);
            return _descriptor;
        }

        /// <summary>
        /// Adds string property to descriptor 
        /// with previously chained name 
        /// </summary>
        /// <param name="splitType">How to split string</param>
        /// <param name="separator">Separator to use</param>
        /// <param name="exclusions">file describing strings to exclude</param>
        /// <returns>descriptor with added property</returns>
        public Descriptor AsString(StringSplitType splitType, string separator = " ", string exclusions = null)
        {
            StringProperty p = new StringProperty();
            p.SplitType = splitType;
            p.Separator = separator;
            p.ImportExclusions(exclusions);
            p.AsEnum = _label;
            AddProperty(p);
            return _descriptor;
        }

        /// <summary>
        /// Adds DateTime property to descriptor
        /// with previously chained name
        /// </summary>
        /// <param name="features">Which date features to use (can pipe: DateTimeFeature.Year | DateTimeFeature.DayOfWeek)</param>
        /// <returns>descriptor with added property</returns>
        public Descriptor AsDateTime(DateTimeFeature features)
        {
            if (_label)
                throw new DescriptorException("Cannot use a DateTime property as a label");

            var p = new DateTimeProperty(features)
            {
                Discrete = true,
                Name = _name
            };

            AddProperty(p);
            return _descriptor;
        }

        /// <summary>
        /// Adds DateTime property to descriptor
        /// with previously chained name
        /// </summary>
        /// <param name="portion">Which date portions to use (can pipe: DateTimeFeature.Year | DateTimeFeature.DayOfWeek)</param>
        /// <returns>descriptor with added property</returns>
        public Descriptor AsDateTime(DatePortion portion)
        {
            if (_label)
                throw new DescriptorException("Cannot use an DateTime property as a label");

            var p = new DateTimeProperty(portion)
            {
                Discrete = true,
                Name = _name
            };

            AddProperty(p);
            return _descriptor;
        }

        /// <summary>
        /// Adds Enumerable property to descriptor
        /// with previousy chained name
        /// </summary>
        /// <param name="length">length of enumerable to expand</param>
        /// <returns>descriptor with added property</returns>
        public Descriptor AsEnumerable(int length)
        {
            if (_label)
                throw new DescriptorException("Cannot use an Enumerable property as a label");

            var p = new EnumerableProperty(length)
            {
                Name = _name,
                Discrete = false
            };

            AddProperty(p);

            return _descriptor;
        }

        private void AddProperty(Property p)
        {
            if (_label)
                _descriptor.Label = p;
            else
            {
                var features = new List<Property>(_descriptor.Features);
                features.Add(p);
                _descriptor.Features = features.ToArray();
            }
        }
    }
}