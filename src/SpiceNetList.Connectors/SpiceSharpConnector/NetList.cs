﻿using SpiceSharp;
using SpiceSharp.Simulations;
using System.Collections.Generic;

namespace SpiceNetlist.Connectors.SpiceSharpConnector
{
    public class NetList
    {
        public string Title { get; set; }

        public Circuit Circuit { get; set; }

        //TODO: Introduce better user parameters system
        public Dictionary<string, double> UserGlobalParameters = new Dictionary<string, double>();

        public string SimulationType { get; set; }

        public BaseConfiguration BaseConfiguration { get; set; }

        public FrequencyConfiguration FrequencyConfiguration { get; set; }

        public TimeConfiguration TimeConfiguration { get; set; }

        public DCConfiguration DCConfiguration { get; set; }

        public List<string> Comments { get; set; }

        public List<string> Warnings { get; set; }

        public List<string> Errors { get; set; }
    }
}