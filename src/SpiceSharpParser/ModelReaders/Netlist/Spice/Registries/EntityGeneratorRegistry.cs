﻿using System;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.EntityGenerators;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Registries
{
    /// <summary>
    /// Registry of <see cref="EntityGenerator"/>.
    /// </summary>
    public class EntityGeneratorRegistry : BaseRegistry<EntityGenerator>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityGeneratorRegistry"/> class.
        /// </summary>
        public EntityGeneratorRegistry()
        {
        }
    }
}
