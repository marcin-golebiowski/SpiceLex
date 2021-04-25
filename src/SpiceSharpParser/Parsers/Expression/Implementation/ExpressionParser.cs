﻿using SpiceSharpBehavioral.Parsers;
using SpiceSharpBehavioral.Parsers.Nodes;
using SpiceSharpParser.Common.Evaluation;
using SpiceSharpParser.ModelReaders.Netlist.Spice;
using SpiceSharpParser.ModelReaders.Netlist.Spice.Context.Names;
using SpiceSharpParser.Parsers.Expression.Implementation;
using SpiceSharpParser.Parsers.Expression.Implementation.ResolverFunctions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpiceSharpParser.Parsers.Expression
{
    public class ExpressionParser
    {
        public ExpressionParser(EvaluationContext context, bool throwOnErrors, ISpiceNetlistCaseSensitivitySettings caseSettings)
        {
            InternalParser = new Parser();
            DoubleBuilder = new CustomRealBuilder(context, InternalParser, caseSettings);

            Context = context;
            ThrowOnErrors = throwOnErrors;
        }

        public EvaluationContext Context { get; }

        public bool ThrowOnErrors { get; }

        protected CustomRealBuilder DoubleBuilder { get; }

        protected Parser InternalParser { get; }

        public IEnumerable<string> GetFunctions(string expression)
        {
            var list = new List<string>();
            var node = InternalParser.Parse(expression);
            DoubleBuilder.FunctionFound += (o, e) =>
            {
                list.Add(e.Function.Name);
                e.Result = 0;
            };

            try
            {
                DoubleBuilder.Build(node);
            }
            catch (Exception)
            {
                if (ThrowOnErrors)
                {
                    throw;
                }
            }

            return list;
        }

        public IEnumerable<string> GetVariables(string expression)
        {
            var list = new List<string>();
            var node = InternalParser.Parse(expression);
            DoubleBuilder.VariableFound += (o, e) =>
            {
                list.Add(e.Node.ToString());
                e.Result = 0;
            };
            try
            {
                DoubleBuilder.Build(node);
            }
            catch (Exception)
            {
                if (ThrowOnErrors)
                {
                    throw;
                }
            }

            return list;
        }

        public double Parse(string expression)
        {
            var node = InternalParser.Parse(expression);
            return DoubleBuilder.Build(node);
        }

        public Node Resolve(string expression)
        {
            var node = InternalParser.Parse(expression);

            var resolver = new Resolver();

            if (Context.NameGenerator?.NodeNameGenerator is SubcircuitNodeNameGenerator sng)
            {
                resolver.UnknownVariableFound += (sender, args) =>
                {
                    if (args.Node.NodeType == NodeTypes.Voltage)
                    {
                        args.Result = VariableNode.Voltage(Context.NameGenerator.ParseNodeName(args.Node.Name));
                    }

                    if (args.Node.NodeType == NodeTypes.Current)
                    {
                        args.Result = VariableNode.Current(Context.NameGenerator.GenerateObjectName(args.Node.Name));
                    }
                };
            }

            resolver.VariableMap = new Dictionary<VariableNode, Node>();
            foreach (var variable in DoubleBuilder.Variables)
            {
                resolver.VariableMap[VariableNode.Variable(variable.Name)] = variable.Value();
            }

            // TIME variable is handled well in SpiceSharpBehavioral
            resolver.VariableMap.Remove(VariableNode.Variable("TIME"));
            resolver.FunctionMap = CreateFunctions();

            var resolved = resolver.Resolve(node);

            return resolved;
        }

        public Dictionary<string, ResolverFunction> CreateFunctions()
        {
            var result = new Dictionary<string, ResolverFunction>();

            foreach (var functionName in Context.FunctionsBody.Keys)
            {
                var body = Context.FunctionsBody[functionName];
                if (body != null)
                {
                    var bodyNode = InternalParser.Parse(body);

                    result[functionName] = new StaticResolverFunction(
                        functionName,
                        bodyNode,
                        Context.FunctionArguments[functionName].Select(a => Node.Variable(a)).ToList());
                }
            }

            result["poly"] = new PolyResolverFunction();
            result["if"] = new IfResolverFunction();

            return result;
        }
    }
}