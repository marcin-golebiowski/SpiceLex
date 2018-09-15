﻿using System.Collections.Generic;
using SpiceSharp.Circuits;
using SpiceSharp.Components;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.EntityGenerators.Models
{
    public class SwitchModelGenerator : ModelGenerator
    {
        /// <summary>
        /// Gets generated Spice types by generator
        /// </summary>
        /// <returns>
        /// Generated Spice Types
        /// </returns>
        public override IEnumerable<string> GetGeneratedTypes()
        {
            return new List<string>() { "sw", "csw" };
        }

        protected override Entity GenerateModel(string name, string type)
        {
            switch (type)
            {
                case "sw": return new VoltageSwitchModel(name);
                case "csw": return new CurrentSwitchModel(name);
            }

            return null;
        }
    }
}
