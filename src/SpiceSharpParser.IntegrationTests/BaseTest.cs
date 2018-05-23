﻿using SpiceSharp.Simulations;
using SpiceSharpParser.Model.Spice;
using SpiceSharpParser.ModelReader.Spice;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SpiceSharpParser.IntegrationTests
{
    public class BaseTest
    {
        /// <summary>
        /// Absolute tolerance used
        /// </summary>
        public double AbsTol = 1e-12;

        /// <summary>
        /// Relative tolerance used
        /// </summary>
        public double RelTol = 1e-3;

        public static SpiceReaderResult ParseNetlistInWorkingDirectory(string workingDirectory, params string[] lines)
        {
            var text = string.Join(Environment.NewLine, lines);
            var parserFront = new ParserFacade();
            var parserResult = parserFront.ParseNetlist(text, new ParserSettings() { HasTitle = true, IsEndRequired = true }, workingDirectory);


            return parserResult.ReaderResult;
        }

        public static SpiceReaderResult ParseNetlist(params string[] lines)
        {
            var text = string.Join(Environment.NewLine, lines);
            var parserFront = new ParserFacade();
            return parserFront.ParseNetlist(text, new ParserSettings() { HasTitle = true, IsEndRequired = true }).ReaderResult;
        }

        public static Netlist ParseNetlistToModel(bool isEndRequired, bool hasTitle, params string[] lines)
        {
            var text = string.Join(Environment.NewLine, lines);
            var parserFront = new ParserFacade();
            return parserFront.ParseNetlist(text, new ParserSettings() { HasTitle = hasTitle, IsEndRequired = isEndRequired }).PreprocessedNetlistModel;
        }

        public static Netlist ParseNetlistToPostProcessedModel(bool isEndRequired, bool hasTitle, params string[] lines)
        {
            var text = string.Join(Environment.NewLine, lines);
            var parserFront = new ParserFacade();
            return parserFront.ParseNetlist(text, new ParserSettings() { HasTitle = hasTitle, IsEndRequired = isEndRequired }).PostprocessedNetlistModel;
        }

        /// <summary>
        /// Runs simulations from <see cref="SpiceReaderResult.Simulations"/>
        /// </summary>
        /// <param name="readerResult">A reader result</param>
        /// <returns>
        /// A list of exports list
        /// </returns>
        public static List<object> RunSimulations(SpiceReaderResult readerResult)
        {
            var result = new List<object>();

            foreach (var export in readerResult.Exports)
            {
                var simulation = export.Simulation;
                if (simulation is DC)
                {
                    var dcResult = new List<double>();
                    result.Add(dcResult);
                    simulation.OnExportSimulationData += (sender, e) =>
                    {
                        dcResult.Add(export.Extract());
                    };
                }

                if (simulation is OP)
                {
                    double opResult = double.NaN;
                    simulation.OnExportSimulationData += (sender, e) =>
                    {
                        opResult = export.Extract();
                    };

                    simulation.FinalizeSimulationExport += (sender, e) =>
                    {
                        result.Add(opResult);
                    };
                }

                if (simulation is Transient)
                {
                    var tranResult = new List<Tuple<double, double>>();
                    result.Add(tranResult);
                    simulation.OnExportSimulationData += (sender, e) =>
                    {
                        tranResult.Add(new Tuple<double, double>(e.Time, export.Extract()));
                    };
                }
            }

            foreach (var simulation in readerResult.Simulations)
            {
                simulation.Run(readerResult.Circuit);
            }

            return result;
        }

        public static double RunOpSimulation(SpiceReaderResult readerResult, string nameOfExport)
        {
            double result = double.NaN;
            var export = readerResult.Exports.Find(e => e.Name.ToLower() == nameOfExport.ToLower()); //TODO: Remove ToLower someday
            var simulation = readerResult.Simulations.Single();
            simulation.OnExportSimulationData += (sender, e) => {

                result = export.Extract();
            };

            simulation.Run(readerResult.Circuit);

            return result;
        }

        public static double[] RunOpSimulation(SpiceReaderResult readerResult, params string[] nameOfExport)
        {
            var simulation = readerResult.Simulations.Single();
            double[] result = new double[nameOfExport.Length];

            simulation.OnExportSimulationData += (sender, e) => {

                for (var i = 0; i < nameOfExport.Length; i++) {
                    var export = readerResult.Exports.Find(exp => exp.Name.ToLower() == nameOfExport[i].ToLower()); //TODO: Remove ToLower someday
                    result[i] = export.Extract();
                }
            };

            simulation.Run(readerResult.Circuit);

            return result;
        }

        public static Tuple<double, double>[] RunTransientSimulation(SpiceReaderResult readerResult, string nameOfExport)
        {
            var list = new List<Tuple<double,double>>();

            var export = readerResult.Exports.Find(e => e.Name.ToLower() == nameOfExport.ToLower()); //TODO: Remove ToLower someday
            var simulation = readerResult.Simulations.Single();
            simulation.OnExportSimulationData += (sender, e) => {

                list.Add(new Tuple<double, double>(e.Time, export.Extract()));
            };

            simulation.Run(readerResult.Circuit);

            return list.ToArray();
        }

        public static Tuple<double, double>[] RunDCSimulation(SpiceReaderResult readerResult, string nameOfExport)
        {
            var list = new List<Tuple<double, double>>();

            var export = readerResult.Exports.Find(e => e.Name.ToLower() == nameOfExport.ToLower()); //TODO: Remove ToLower someday
            var simulation = readerResult.Simulations.Single();
            simulation.OnExportSimulationData += (sender, e) => {

                list.Add(new Tuple<double, double>(e.SweepValue, export.Extract()));
            };

            simulation.Run(readerResult.Circuit);

            return list.ToArray();
        }

        protected void Compare(IEnumerable<Tuple<double, double>> exports, IEnumerable<Func<double, double>> references)
        {
            using (var exportIt = exports.GetEnumerator())
            using (var referencesIt = references.GetEnumerator())
            {
                while (exportIt.MoveNext() && referencesIt.MoveNext())
                {
                    double actual = exportIt.Current.Item2;
                    double expected = referencesIt.Current(exportIt.Current.Item1);
                    double tol = Math.Max(Math.Abs(actual), Math.Abs(expected)) * RelTol + AbsTol;
                    Assert.True(Math.Abs(expected - actual) < tol);
                }
            }
        }

        protected void Compare(double actual, double expected)
        {
            double tol = Math.Max(Math.Abs(actual), Math.Abs(expected)) * RelTol + AbsTol;
            Assert.True(Math.Abs(expected - actual) < tol, $"Actual={actual} expected={expected}");
        }

        protected void Compare(IEnumerable<Tuple<double, double>> exports, IEnumerable<double> references)
        {
            using (var exportIt = exports.GetEnumerator())
            using (var referencesIt = references.GetEnumerator())
            {
                while (exportIt.MoveNext() && referencesIt.MoveNext())
                {
                    double actual = exportIt.Current.Item2;
                    double expected = referencesIt.Current;
                    double tol = Math.Max(Math.Abs(actual), Math.Abs(expected)) * RelTol + AbsTol;
                    Assert.True(Math.Abs(expected - actual) < tol);
                }
            }
        }

        protected void Compare(IEnumerable<double> exports, IEnumerable<double> references)
        {
            using (var exportIt = exports.GetEnumerator())
            using (var referencesIt = references.GetEnumerator())
            {
                while (exportIt.MoveNext() && referencesIt.MoveNext())
                {
                    double actual = exportIt.Current;
                    double expected = referencesIt.Current;
                    double tol = Math.Max(Math.Abs(actual), Math.Abs(expected)) * RelTol + AbsTol;
                    Assert.True(Math.Abs(expected - actual) < tol);
                }
            }
        }
    }
}
