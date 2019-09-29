﻿using System;
using SpiceSharp.Simulations;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Context;
using SpiceSharpParser.Models.Netlist.Spice.Objects;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.Controls.Simulations
{
    public interface ISimulationsFactory
    {
        void Create(Control statement, IReadingContext context, Func<string, Control, IReadingContext, BaseSimulation> createSimulation);
    }
}