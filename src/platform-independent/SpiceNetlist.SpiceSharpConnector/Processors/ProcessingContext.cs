﻿using System;
using System.Collections.Generic;
using SpiceNetlist.SpiceObjects;
using SpiceNetlist.SpiceObjects.Parameters;
using SpiceNetlist.SpiceSharpConnector.Processors.Controls.Plots;
using SpiceNetlist.SpiceSharpConnector.Processors.Evaluation;
using SpiceSharp;
using SpiceSharp.Circuits;
using SpiceSharp.Parser.Readers;
using SpiceSharp.Simulations;

namespace SpiceNetlist.SpiceSharpConnector.Processors
{
    /// <summary>
    /// Processing context
    /// </summary>
    public class ProcessingContext : IProcessingContext
    {
        private string currentPath = null;

        public ProcessingContext()
        {
            ContextName = string.Empty;
            Netlist = new Netlist(new Circuit(), string.Empty);
            AvailableSubcircuits = new List<SubCircuit>();
            Evaluator = new Evaluator();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingContext"/> class.
        /// </summary>
        /// <param name="contextName">The name of context</param>
        /// <param name="netlist">The netlist for context</param>
        public ProcessingContext(string contextName, Netlist netlist)
        {
            ContextName = contextName;
            Netlist = netlist;
            AvailableSubcircuits = new List<SubCircuit>();
            Evaluator = new Evaluator();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessingContext"/> class.
        /// </summary>
        public ProcessingContext(string contextName,
            IProcessingContext parent, 
            SubCircuit currentSubciruit, 
            List<string> pinInstanceNames, 
            Dictionary<string, double> availableParameters)
        {
            ContextName = contextName;
            Netlist = parent.Netlist;
            Parent = parent;
            CurrrentSubCircuit = currentSubciruit;
            PinInstanceNames = pinInstanceNames;
            AvailableSubcircuits = new List<SubCircuit>();
            AvailableSubcircuits.AddRange(parent.AvailableSubcircuits);
            Evaluator = new Evaluator(parent.Evaluator);
        }

        /// <summary>
        /// Gets the evaluator
        /// </summary>
        public IEvaluator Evaluator { get; }

        /// <summary>
        /// Gets the current simulation configuration
        /// </summary>
        public SimulationConfiguration SimulationConfiguration { get; } = new SimulationConfiguration();

        /// <summary>
        /// Gets the available definions of subcircuits
        /// </summary>
        public List<SubCircuit> AvailableSubcircuits { get; }

        /// <summary>
        /// Gets the created simulations
        /// </summary>
        public IEnumerable<Simulation> Simulations
        {
            get
            {
                return Netlist.Simulations;
            }
        }

        /// <summary>
        /// Gets the netlist
        /// </summary>
        public Netlist Netlist { get; }

        /// <summary>
        /// Gets the context name
        /// </summary>
        public string ContextName { get; }

        /// <summary>
        /// Gets the current subcircuit
        /// </summary>
        protected SubCircuit CurrrentSubCircuit { get; }

        /// <summary>
        /// Gets the names of pinds for the current subcircuit
        /// </summary>
        protected List<string> PinInstanceNames { get; }

        /// <summary>
        /// Gets or sets the parent of the context
        /// </summary>
        public IProcessingContext Parent { get; set; }

        /// <summary>
        /// Gets the path of the context
        /// </summary>
        public string Path
        {
            get
            {
                if (currentPath == null)
                {
                    List<string> path = new List<string>() { ContextName };

                    IProcessingContext context = this;
                    while (context.Parent != null)
                    {
                        path.Insert(0, context.Parent.ContextName);
                        context = context.Parent;
                    }

                    var result = string.Empty;
                    foreach (var pathPart in path)
                    {
                        if (pathPart != string.Empty)
                        {
                            result += pathPart + ".";
                        }
                    }

                    currentPath = result;
                }

                return currentPath;
            }
        }

        /// <summary>
        /// Adds warning
        /// </summary>
        public void AddWarning(string warning)
        {
            this.Netlist.Warnings.Add(warning);
        }

        /// <summary>
        /// Adds comment
        /// </summary>
        public void AddComment(CommentLine statement)
        {
            this.Netlist.Comments.Add(statement.Text);
        }

        /// <summary>
        /// Adds export to netlist
        /// </summary>
        public void AddExport(Export export)
        {
            Netlist.Exports.Add(export);
        }

        /// <summary>
        /// Adds plot to netlist
        /// </summary>
        public void AddPlot(Plot plot)
        {
            Netlist.Plots.Add(plot);
        }

        /// <summary>
        /// Adds entity to netlist
        /// </summary>
        public void AddEntity(Entity entity)
        {
            Netlist.Circuit.Objects.Add(entity);
        }

        /// <summary>
        /// Adds simulation to netlist
        /// </summary>
        public void AddSimulation(BaseSimulation simulation)
        {
            Netlist.Simulations.Add(simulation);
        }

        /// <summary>
        /// Sets voltage initial condition for node
        /// </summary>
        public void SetICVoltage(string nodeName, string value)
        {
            Netlist.Circuit.Nodes.InitialConditions[this.GenerateNodeName(nodeName)] = Evaluator.EvaluateDouble(value);
        }

        /// <summary>
        /// Evaluates the value to double
        /// </summary>
        /// <param name="expression">Expressin to evaluate</param>
        /// <returns>
        /// A value of expression
        /// </returns>
        public double ParseDouble(string expression)
        {
            try
            {
                return Evaluator.EvaluateDouble(expression);
            }
            catch (Exception)
            {
                throw new Exception("Exception during evaluation of expression: " + expression);
            }
        }

        /// <summary>
        /// Sets the parameter of entity and enables updates
        /// </summary>
        /// <param name="entity">An entity of parameter</param>
        /// <param name="propertyName">A property name</param>
        /// <param name="expression">An expression</param>
        public void SetParameter(Entity entity, string propertyName, string expression)
        {
            entity.SetParameter(propertyName, Evaluator.EvaluateDouble(expression));

            var setter = entity.ParameterSets.GetSetter(propertyName);
            // re-evaluation makes sense only if there is a setter
            if (setter != null)
            {
                Evaluator.AddDynamicExpression(new DoubleExpression(expression, setter));
            }
        }

        /// <summary>
        /// Find model in the context and in parent contexts
        /// </summary>
        public T FindModel<T>(string modelName)
            where T : Entity
        {
            IProcessingContext context = this;
            while (context != null)
            {
                var modelNameToSearch = (context.Path + modelName).ToLower();

                Entity model;
                if (Netlist.Circuit.Objects.TryGetEntity(new Identifier(modelNameToSearch), out model))
                {
                    return (T)model;
                }

                context = context.Parent;
            }

            return null;
        }

        /// <summary>
        /// Sets entity parameters
        /// </summary>
        public void SetParameters(Entity entity, ParameterCollection parameters, int toSkip = 0)
        {
            foreach (SpiceObjects.Parameter parameter in parameters.Skip(toSkip).Take(parameters.Count - toSkip))
            {
                if (parameter is AssignmentParameter ap)
                {
                    try
                    {
                        this.SetParameter(entity, ap.Name, ap.Value);
                    }
                    catch (Exception ex)
                    {
                        Netlist.Warnings.Add(ex.ToString());
                    }
                }
                else
                {
                    Netlist.Warnings.Add("Unknown parameter: " + parameter.Image);
                }
            }
        }

        /// <summary>
        /// Creates nodes for component
        /// </summary>
        public void CreateNodes(SpiceSharp.Components.Component component, ParameterCollection parameters)
        {
            Identifier[] nodes = new Identifier[component.PinCount];
            for (var i = 0; i < component.PinCount; i++)
            {
                string pinName = parameters.GetString(i);
                nodes[i] = GenerateNodeName(pinName);
            }

            component.Connect(nodes);
        }

        /// <summary>
        /// Generates object name for current context
        /// </summary>
        public string GenerateObjectName(string objectName)
        {
            return Path + objectName;
        }

        /// <summary>
        /// Generates node name for current context
        /// </summary>
        public string GenerateNodeName(string pinName)
        {
            if (pinName == "0" || pinName == "gnd" || pinName == "GND")
            {
                return pinName.ToUpper();
            }

            if (CurrrentSubCircuit != null)
            {
                Dictionary<string, string> map = new Dictionary<string, string>();

                for (var i = 0; i < this.CurrrentSubCircuit.Pins.Count; i++)
                {
                    map[CurrrentSubCircuit.Pins[i]] = this.PinInstanceNames[i];
                }

                if (map.ContainsKey(pinName))
                {
                    return map[pinName].ToLower();
                }
            }

            return (Path + pinName).ToLower();
        }
    }
}
