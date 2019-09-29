﻿using System.Collections.Generic;
using System.Linq;
using SpiceSharp.Components;
using SpiceSharpBehavioral.Parsers;
using SpiceSharpParser.Common;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Context;
using SpiceSharpParser.Models.Netlist.Spice.Objects;
using SpiceSharpParser.Models.Netlist.Spice.Objects.Parameters;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.EntityGenerators.Components.Sources
{
    public class ArbitraryBehavioralGenerator : SourceGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArbitraryBehavioralGenerator"/> class.
        /// </summary>
        public ArbitraryBehavioralGenerator()
        {
        }

        /// <summary>
        /// Gets generated types.
        /// </summary>
        /// <returns>
        /// Generated types.
        /// </returns>
        public override IEnumerable<string> GeneratedTypes => new List<string>() {"B"};

        public override SpiceSharp.Components.Component Generate(string componentIdentifier, string originalName,
            string type, ParameterCollection parameters, IReadingContext context)
        {
            var parser = CreateParser(context);

            if (parameters.Any(p => p is AssignmentParameter asgParameter && asgParameter.Name.ToLower() == "v"))
            {
                var expressionParameter = (AssignmentParameter) parameters.First(p => p is AssignmentParameter asgParameter && asgParameter.Name.ToLower() == "v");
                        
                var entity = new BehavioralVoltageSource(componentIdentifier);
                entity.SetParameter("parser", parser);

                context.CreateNodes(entity, parameters);
                var baseParameters = entity.ParameterSets.Get<SpiceSharpBehavioral.Components.BehavioralBehaviors.BaseParameters>();
                baseParameters.Expression = expressionParameter.Value;

                return entity;
            }

            if (parameters.Any(p => p is AssignmentParameter asgParameter && asgParameter.Name.ToLower() == "i"))
            {
                var expressionParameter = (AssignmentParameter) parameters.First(p =>
                    p is AssignmentParameter asgParameter && asgParameter.Name.ToLower() == "i");

                var entity = new BehavioralCurrentSource(componentIdentifier);
                entity.SetParameter("parser", parser);

                context.CreateNodes(entity, parameters);
                var baseParameters = entity.ParameterSets.Get<SpiceSharpBehavioral.Components.BehavioralBehaviors.BaseParameters>();
                baseParameters.Expression = expressionParameter.Value;

                return entity;
            }

            return null;
        }
    }
}