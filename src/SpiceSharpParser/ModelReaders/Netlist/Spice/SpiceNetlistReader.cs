﻿using SpiceSharp;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Context;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Evaluation;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Readers;
using SpiceSharpParser.Models.Netlist.Spice;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice
{
    /// <summary>
    /// Translates a SPICE model to SpiceSharp model.
    /// </summary>
    public class SpiceNetlistReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpiceNetlistReader"/> class.
        /// </summary>
        public SpiceNetlistReader(SpiceNetlistReaderSettings settings)
        {
            Settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Gets the settings of the reader.
        /// </summary>
        public SpiceNetlistReaderSettings Settings { get; }

        /// <summary>
        /// Translates Netlist object mode to SpiceSharp netlist.
        /// </summary>
        /// <param name="netlist">A object model of the netlist.</param>
        /// <returns>
        /// A new SpiceSharp netlist.
        /// </returns>
        public SpiceNetlistReaderResult Read(SpiceNetlist netlist)
        {
            if (netlist == null)
            {
                throw new System.ArgumentNullException(nameof(netlist));
            }

            // Create result netlist
            var result = new SpiceNetlistReaderResult(new Circuit(), netlist.Title);

            // Create reading context
            var resultService = new ResultService(result);
            var nodeNameGenerator = new MainCircuitNodeNameGenerator(new string[] { "0" });
            var objectNameGenerator = new ObjectNameGenerator(string.Empty);
            var readingEvaluator = new SpiceEvaluator("Main reading evaluator", null, Settings.EvaluatorMode, Settings.Seed, Settings.Entities.Exporters, nodeNameGenerator, objectNameGenerator);
            var simulationContexts = new SimulationContexts(resultService, readingEvaluator);

            var readingContext = new ReadingContext(
                string.Empty,
                simulationContexts,
                readingEvaluator,
                resultService,
                nodeNameGenerator,
                objectNameGenerator,
                Settings.Entities);

            readingContext.ReadersRegistry = CreateReaderRegistry(Settings.Entities);

            // Read statements form input netlist using created context
            var statementsReader = new StatementsReader();
            statementsReader.Read(netlist.Statements, readingContext, Settings.Orderer);

            // Return and update evaluators info.
            result.Seed = result.Seed ?? Settings.Seed;

            // TODO: please, please, do something about below lines
            simulationContexts.Prepare();
            result.Evaluators = simulationContexts.GetSimulationEvaluators();

            return result;
        }

        private ISpiceReaderRegistry CreateReaderRegistry(ISpiceEntityRegistry enityRegistry)
        {
            return new SpiceReaderRegistry(enityRegistry.WaveForms, enityRegistry.Controls, enityRegistry.Models, enityRegistry.Components);
        }
    }
}
