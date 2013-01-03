﻿using System;
using numl.Utils;
using System.Linq;
using numl.Supervised;
using numl.Math.Probability;
using System.Threading.Tasks;
using numl.Math.LinearAlgebra;
using System.Collections.Generic;

namespace numl
{
    public static class Learner
    {
        public static LearningModel Best(this IEnumerable<LearningModel> models)
        {
            var q = from m in models
                    where m.Accuracy == (models.Select(s => s.Accuracy).Max())
                    select m;

            return q.FirstOrDefault();
        }

        public static LearningModel[] Learn(IEnumerable<object> examples, double trainingPercentage, params IGenerator[] generators)
        {
            if (generators.Length == 0)
                throw new InvalidOperationException("Need to have at least one generator!");

            // for randomizing test sets
            MLRandom.SetSeedFromSystemTime();

            // get first descriptor (they should all be the same)
            var descriptor = generators[0].Descriptor;
            var total = examples.Count();
            
            // expand data
            var data = descriptor.Convert(examples).ToExamples();
            Matrix x = data.Item1;
            Vector y = data.Item2;

            var models = new LearningModel[generators.Length];

            // run in parallel since they all have 
            // read-only references to the data model
            // and update indices independently
            Parallel.For(0, models.Length, i =>
            {
                var t = GenerateModel(generators[i], x, y, examples, trainingPercentage);

                models[i].Generator = generators[i];
                models[i].Model = t.Model;
                models[i].Accuracy = t.Accuracy;
            });

            return models;
            
        }

        public static LearningModel Repeat(IEnumerable<object> examples, double trainingPercentage, IGenerator generator, int repeat)
        {
            MLRandom.SetSeedFromSystemTime();
            var total = examples.Count();
            var descriptor = generator.Descriptor;
            var data = descriptor.Convert(examples).ToExamples();

            Matrix x = data.Item1;
            Vector y = data.Item2;

            var models = new IModel[repeat];
            var accuracy = Vector.Zeros(repeat);

            // run in parallel since they all have 
            // read-only references to the data model
            // and update indices independently
            Parallel.For(0, models.Length, i =>
            {
                var t = GenerateModel(generator, x, y, examples, trainingPercentage);
                models[i] = t.Model;
                accuracy[i] = t.Accuracy;
            });

            var idx = accuracy.MaxIndex();

            return new LearningModel { Generator = generator, Model = models[idx], Accuracy = accuracy[idx] };
        }

        private static LearningModel GenerateModel(IGenerator generator, Matrix x, Vector y, IEnumerable<object> examples, double trainingPct)
        {
            var descriptor = generator.Descriptor;
            var total = examples.Count();
            var trainingCount = (int)System.Math.Floor(total * trainingPct);

            // 100 - trainingPercentage for testing
            var testingSlice = GetTestPoints(total - trainingCount, total).ToArray();

            // trainingPercentage for training
            var trainingSlice = GetTrainingPoints(testingSlice, total);

            // training
            var x_t = x.Slice(trainingSlice);
            var y_t = y.Slice(trainingSlice);

            // generate model
            var model = generator.Generate(x_t, y_t);
            model.Descriptor = descriptor;

            // testing            
            object[] test = GetTestExamples(testingSlice, examples);
            double accuracy = 0;

            for (int j = 0; j < test.Length; j++)
            {
                // items under test
                object o = test[j];

                // get truth
                var truth = FastReflection.Get(o, descriptor.Label.Name);

                // make prediction
                var features = descriptor.Convert(o, false).ToVector();

                var p = model.Predict(features);
                var pred = descriptor.Label.Convert(p);

                // assess accuracy
                if (truth.Equals(pred))
                    accuracy += 1;
            }

            // get percentage correct
            accuracy /= test.Length;

            return new LearningModel { Generator = generator, Model = model, Accuracy = accuracy };
        }

        private static object[] GetTestExamples(IEnumerable<int> slice, IEnumerable<object> examples)
        {
            return examples
                    .Where((o, i) => slice.Contains(i))
                    .ToArray();
        }

        private static IEnumerable<int> GetTestPoints(int testCount, int total)
        {
            List<int> taken = new List<int>(testCount);
            while (taken.Count < testCount)
            {
                int i = MLRandom.GetUniform(total);
                if (!taken.Contains(i))
                {
                    taken.Add(i);
                    yield return i;
                }
            }
        }

        private static IEnumerable<int> GetTrainingPoints(IEnumerable<int> testPoints, int total)
        {
            for (int i = 0; i < total; i++)
                if (!testPoints.Contains(i))
                    yield return i;
        }
    }

    public class LearningModel
    {
        public IGenerator Generator { get; set; }
        public IModel Model { get; set; }
        public double Accuracy { get; set; }
    }
}
