﻿using System;
using System.Collections.Generic;
using SpiceSharp;
using SpiceSharp.Simulations;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Context;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Exceptions;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.Controls.Exporters.VoltageExports;
using SpiceSharpParser.Models.Netlist.Spice.Objects;
using SpiceSharpParser.Models.Netlist.Spice.Objects.Parameters;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.Controls.Exporters
{
    /// <summary>
    /// Generates voltage <see cref="Export"/>
    /// </summary>
    public class VoltageExporter : Exporter
    {
        /// <summary>
        /// Creates a new voltage export
        /// </summary>
        /// <param name="type">A type of export</param>
        /// <param name="parameters">A parameters of export</param>
        /// <param name="simulation">A simulation for export</param>
        /// <returns>
        /// A new export
        /// </returns>
        public override Export CreateExport(string type, ParameterCollection parameters, Simulation simulation, INodeNameGenerator nodeNameGenerator, IObjectNameGenerator objectNameGenerator, bool ignoreCaseForNodes)
        {
            if (parameters.Count != 1 || (!(parameters[0] is VectorParameter) && !(parameters[0] is SingleParameter)))
            {
                throw new WrongParameterException("Voltage exports should have vector or single parameter");
            }

            // Get the nodes
            Identifier node, reference = null;
            string nodePath = null, referencePath = null;

            if (parameters[0] is VectorParameter vector)
            {
                switch (vector.Elements.Count)
                {
                    case 0:
                        throw new WrongParametersCountException("No nodes for voltage export. Node expected");
                    case 2:
                        referencePath = vector.Elements[1].Image;
                        reference = new StringIdentifier(nodeNameGenerator.Parse(referencePath));
                        goto case 1;
                    case 1:
                        nodePath = vector.Elements[0].Image;
                        node = new StringIdentifier(nodeNameGenerator.Parse(nodePath));
                        break;
                    default:
                        throw new WrongParametersCountException("Too many nodes specified for voltage export");
                }
            }
            else
            {
                nodePath = parameters.GetString(0);
                node = new StringIdentifier(nodeNameGenerator.Parse(nodePath));
            }

            Export ve = null;
            switch (type.ToLower())
            {
                case "v": ve = new VoltageExport(simulation, node, reference, nodePath, referencePath); break;
                case "vr": ve = new VoltageRealExport(simulation, node, reference, nodePath, referencePath); break;
                case "vi": ve = new VoltageImaginaryExport(simulation, node, reference, nodePath, referencePath); break;
                case "vm": ve = new VoltageMagnitudeExport(simulation, node, reference, nodePath, referencePath); break;
                case "vdb": ve = new VoltageDecibelExport(simulation, node, reference, nodePath, referencePath); break;
                case "vph":
                case "vp": ve = new VoltagePhaseExport(simulation, node, reference, nodePath, referencePath); break;
            }

            return ve;
        }

        /// <summary>
        /// Gets supported voltage exports
        /// </summary>
        /// <returns>
        /// A list of supported voltage exports
        /// </returns>
        public override ICollection<string> GetSupportedTypes()
        {
            return new List<string>() { "v", "vr", "vi", "vm", "vdb", "vp", "vph" };
        }

    }
}
