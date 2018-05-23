﻿using System;
using System.Collections.Generic;
using SpiceSharpParser.Lexer.Spice;
using SpiceSharpParser.Model.Spice;
using SpiceSharpParser.Model.Spice.Objects;
using SpiceSharpParser.Model.Spice.Objects.Parameters;

namespace SpiceSharpParser.Parser.Spice
{
    /// <summary>
    /// Translates a parse tree (<see cref="ParseTreeNode"/> to Spice Object Model - SpiceNetlist library
    /// </summary>
    public class ParseTreeEvaluator
    {
        /// <summary>
        /// The dictionary with tree node values
        /// </summary>
        private Dictionary<ParseTreeNode, ParseTreeNodeEvaluationValue> treeNodesValues = new Dictionary<ParseTreeNode, ParseTreeNodeEvaluationValue>();

        /// <summary>
        /// The dictionary with non-terminal nodes evaluators
        /// </summary>
        private Dictionary<string, Func<ParseTreeNodeEvaluationValues, SpiceObject>> translators = new Dictionary<string, Func<ParseTreeNodeEvaluationValues, SpiceObject>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseTreeEvaluator"/> class.
        /// </summary>
        public ParseTreeEvaluator()
        {
            translators.Add(Symbols.NETLIST, (ParseTreeNodeEvaluationValues nt) => CreateNetlist(nt));
            translators.Add(Symbols.NETLIST_WITHOUT_TITLE, (ParseTreeNodeEvaluationValues nt) => CreateNetlistWithoutTitle(nt));
            translators.Add(Symbols.NETLIST_ENDING, (ParseTreeNodeEvaluationValues nt) => null);
            translators.Add(Symbols.STATEMENTS, (ParseTreeNodeEvaluationValues nt) => CreateStatements(nt));
            translators.Add(Symbols.STATEMENT, (ParseTreeNodeEvaluationValues nt) => CreateStatement(nt));
            translators.Add(Symbols.MODEL, (ParseTreeNodeEvaluationValues nt) => CreateModel(nt));
            translators.Add(Symbols.CONTROL, (ParseTreeNodeEvaluationValues nt) => CreateControl(nt));
            translators.Add(Symbols.COMPONENT, (ParseTreeNodeEvaluationValues nt) => CreateComponent(nt));
            translators.Add(Symbols.PARAMETERS, (ParseTreeNodeEvaluationValues nt) => CreateParameters(nt));
            translators.Add(Symbols.PARAMETER, (ParseTreeNodeEvaluationValues nt) => CreateParameter(nt));
            translators.Add(Symbols.VECTOR, (ParseTreeNodeEvaluationValues nt) => CreateVector(nt));
            translators.Add(Symbols.VECTOR_CONTINUE, (ParseTreeNodeEvaluationValues nt) => CreateVectorContinue(nt));
            translators.Add(Symbols.PARAMETER_BRACKET, (ParseTreeNodeEvaluationValues nt) => CreateBracketParameter(nt));
            translators.Add(Symbols.PARAMETER_BRACKET_CONTENT, (ParseTreeNodeEvaluationValues nt) => CreateBracketParameterContent(nt));
            translators.Add(Symbols.PARAMETER_EQUAL, (ParseTreeNodeEvaluationValues nt) => CreateAssigmentParameter(nt));
            translators.Add(Symbols.PARAMETER_EQUAL_SINGLE, (ParseTreeNodeEvaluationValues nt) => CreateAssigmentSimpleParameter(nt));
            translators.Add(Symbols.PARAMETER_SINGLE, (ParseTreeNodeEvaluationValues nt) => CreateParameterSingle(nt));
            translators.Add(Symbols.SUBCKT, (ParseTreeNodeEvaluationValues nt) => CreateSubCircuit(nt));
            translators.Add(Symbols.SUBCKT_ENDING, (ParseTreeNodeEvaluationValues nt) => null);
            translators.Add(Symbols.COMMENT_LINE, (ParseTreeNodeEvaluationValues nt) => CreateComment(nt));
            translators.Add(Symbols.NEW_LINE, (ParseTreeNodeEvaluationValues nt) => null);
        }

        /// <summary>
        /// Translates a spice parse tree to a context (SpiceNetlist library)
        /// </summary>
        /// <param name="root">A parse tree root</param>
        /// <returns>A net list</returns>
        public SpiceObject Evaluate(ParseTreeNode root)
        {
            var travelsal = new ParseTreeTravelsal();

            // Get tree nodes in post order
            var treeNodes = travelsal.GetIterativePostOrder(root);

            // Iterate over tree nodes
            foreach (var treeNode in treeNodes)
            {
                if (treeNode is ParseTreeNonTerminalNode nt)
                {
                    var items = new ParseTreeNodeEvaluationValues();

                    foreach (var child in nt.Children)
                    {
                        items.Add(treeNodesValues[child]);
                    }

                    if (!translators.ContainsKey(nt.Name))
                    {
                        throw new ParseTreeEvaluationException("Unsupported evaluation of parse tree node");
                    }

                    var treeNodeResult = translators[nt.Name](items);
                    treeNodesValues[treeNode] = new ParseTreeNonTerminalEvaluationValue
                    {
                        SpiceObject = treeNodeResult,
                        Node = treeNode
                    };
                }
                else
                {
                    treeNodesValues[treeNode] = new ParseTreeNodeTerminalTranslationValue()
                    {
                        Node = treeNode,
                        Token = ((ParseTreeTerminalNode)treeNode).Token
                    };
                }
            }

            if (treeNodesValues[root] is ParseTreeNonTerminalEvaluationValue rootNt)
            {
                return rootNt.SpiceObject;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="Netlist"/>
        /// from the values of children nodes of <see cref="Symbols.NETLIST"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="Netlist"/>
        /// </returns>
        private SpiceObject CreateNetlist(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count == 3)
            {
                return new Netlist()
                {
                    Title = string.Empty,
                    Statements = values.GetSpiceObject<Statements>(1)
                };
            }
            else
            {
                return new Netlist()
                {
                    Title = values.GetLexem(0),
                    Statements = values.GetSpiceObject<Statements>(2)
                };
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="Netlist"/>
        /// from the values of children nodes of <see cref="Symbols.NETLIST"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="Netlist"/>
        /// </returns>
        private SpiceObject CreateNetlistWithoutTitle(ParseTreeNodeEvaluationValues values)
        {
            return new Netlist()
            {
                Title = null,
                Statements = values.GetSpiceObject<Statements>(0)
            };
        }

        /// <summary>
        /// Returns new instance of <see cref="SingleParameter"/>
        /// or <see cref="BracketParameter"/>
        /// or <see cref="AssignmentParameter"/>
        /// or <see cref="VectorParameter"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="SingleParameter"/>
        /// </returns>
        private SpiceObject CreateParameter(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count == 1)
            {
                if (values.TryToGetSpiceObject(0, out VectorParameter vp))
                {
                    return vp;
                }

                if (values.TryToGetSpiceObject(0, out SingleParameter sp))
                {
                    return sp;
                }

                if (values.TryToGetSpiceObject(0, out BracketParameter bp))
                {
                    return bp;
                }

                if (values.TryToGetSpiceObject(0, out AssignmentParameter ap))
                {
                    return ap;
                }
            }

            throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
        }

        /// <summary>
        /// Returns new instance of <see cref="ReferenceParameter"/>
        /// or <see cref="ValueParameter"/> or <see cref="WordParameter"/>
        /// or <see cref="ExpressionParameter"/> or <see cref="IdentifierParameter"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER_SINGLE"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="SingleParameter"/>
        /// </returns>
        private SpiceObject CreateParameterSingle(ParseTreeNodeEvaluationValues values)
        {
            if (values[0] is ParseTreeNodeTerminalTranslationValue t)
            {
                var lexemValue = t.Token.Lexem;
                switch (t.Token.TokenType)
                {
                    case (int)SpiceTokenType.REFERENCE:
                        return new ReferenceParameter(lexemValue);
                    case (int)SpiceTokenType.DOUBLE_QUOTED_STRING:
                        return new StringParameter(lexemValue.Trim('"'));
                    case (int)SpiceTokenType.SINGLE_QUOTED_STRING:
                        return new StringParameter(lexemValue.Trim('\''));
                    case (int)SpiceTokenType.VALUE:
                        return new ValueParameter(lexemValue);
                    case (int)SpiceTokenType.WORD:
                        return new WordParameter(lexemValue);
                    case (int)SpiceTokenType.IDENTIFIER:
                        return new IdentifierParameter(lexemValue);
                    case (int)SpiceTokenType.EXPRESSION_BRACKET:
                        return new ExpressionParameter(lexemValue.Trim('{', '}'));
                    case (int)SpiceTokenType.EXPRESSION_SINGLE_QUOTES:
                        return new ExpressionParameter(lexemValue.Trim('\''));
                }
            }

            throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
        }

        /// <summary>
        /// Returns new instance of <see cref="ParameterCollection"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETERS"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="ParameterCollection"/>
        /// </returns>
        private SpiceObject CreateParameters(ParseTreeNodeEvaluationValues values)
        {
            var parameters = new ParameterCollection();

            if (values.Count == 2)
            {
                parameters.Add(values.GetSpiceObject<Parameter>(0));
                parameters.Merge(values.GetSpiceObject<ParameterCollection>(1));
            }

            return parameters;
        }

        /// <summary>
        /// Creates an instance of <see cref="Component"/>
        /// from the values of children nodes of <see cref="Symbols.COMPONENT"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="Component"/>
        /// </returns>
        private SpiceObject CreateComponent(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count != 2 && values.Count != 3)
            {
                throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
            }

            var component = new Component();
            component.Name = values.GetLexem(0);
            component.PinsAndParameters = values.GetSpiceObject<ParameterCollection>(1);
            component.LineNumber = values.GetLexemLineNumber(0);
            return component;
        }

        /// <summary>
        /// Returns new instance of <see cref="Control"/>
        /// from the values of children nodes of <see cref="Symbols.CONTROL"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="Control"/>
        /// </returns>
        private SpiceObject CreateControl(ParseTreeNodeEvaluationValues values)
        {
            var control = new Control();

            switch (values.GetLexem(0).ToLower())
            {
                case ".endl":
                    control.Name = "endl";
                    control.Parameters = new ParameterCollection(); //TODO: fix it, endl can have a parameter
                    control.LineNumber = values.GetLexemLineNumber(0);
                    break;
                case ".if":
                    control.Name = "if";
                    control.Parameters = new ParameterCollection() { new ExpressionParameter(values.GetLexem(1).Trim('(', ')')) };
                    control.LineNumber = values.GetLexemLineNumber(0);
                    break;
                case ".elseif":
                    control.Name = "elseif";
                    control.Parameters = new ParameterCollection() { new ExpressionParameter(values.GetLexem(1).Trim('(', ')')) };
                    control.LineNumber = values.GetLexemLineNumber(0);
                    break;
                case ".else":
                    control.Name = "else";
                    control.Parameters = new ParameterCollection();
                    control.LineNumber = values.GetLexemLineNumber(0);
                    break;
                case ".endif":
                    control.Name = "endif";
                    control.Parameters = new ParameterCollection();
                    control.LineNumber = values.GetLexemLineNumber(0);
                    break;
                default:
                    control.Name = values.GetLexem(1);
                    control.Parameters = values.GetSpiceObject<ParameterCollection>(2);
                    control.LineNumber = values.GetLexemLineNumber(1);
                    break;
            }

            return control;
        }

        /// <summary>
        /// Returns new instance of <see cref="SubCircuit"/>
        /// from the values of children nodes of <see cref="Symbols.SUBCKT"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="SubCircuit"/>
        /// </returns>
        private SpiceObject CreateSubCircuit(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count < 3)
            {
                throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
            }

            var subCkt = new SubCircuit();
            subCkt.Name = values.GetLexem(2);
            subCkt.LineNumber = values.GetLexemLineNumber(2);

            var allParameters = values.GetSpiceObject<ParameterCollection>(3);

            // Parse nodes and parameters
            bool mode = true; // true = nodes, false = parameters
            foreach (var parameter in allParameters)
            {
                if (mode)
                {
                    // After this, only parameters will follow
                    if (parameter is SingleParameter s && s.Image.ToLower() == "params:")
                    {
                        mode = false;
                    }

                    // Parameters have started, so we will keep reading parameters
                    else if (parameter is AssignmentParameter a)
                    {
                        mode = false;
                        subCkt.DefaultParameters.Add(a);
                    }

                    // Still reading nodes
                    else if (parameter is SingleParameter s2)
                    {
                        if (s2 is WordParameter
                            || s2 is IdentifierParameter
                            || int.TryParse(s2.Image, out _))
                        {
                            subCkt.Pins.Add(s2.Image);
                        }
                    }
                }
                else if (parameter is AssignmentParameter a2)
                {
                    subCkt.DefaultParameters.Add(a2);
                }
            }

            subCkt.Statements = values.GetSpiceObject<Statements>(5);
            return subCkt;
        }

        /// <summary>
        /// Returns new instance of <see cref="CommentLine"/>
        /// from the values of children nodes of <see cref="Symbols.COMMENT_LINE"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="CommentLine"/>
        /// </returns>
        private SpiceObject CreateComment(ParseTreeNodeEvaluationValues values)
        {
            var comment = new CommentLine();
            comment.Text = values.GetLexem(0);
            comment.LineNumber = values.GetLexemLineNumber(0);
            return comment;
        }

        /// <summary>
        /// Returns new instance of <see cref="Statement"/>
        /// from the values of children nodes of <see cref="Symbols.STATEMENT"/> parse tree node
        /// </summary>
        /// <returns>
        /// A instance of <see cref="Statement"/>
        /// </returns>
        private SpiceObject CreateStatement(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count != 3 && values.Count != 2)
            {
                throw new ParseTreeEvaluationException("Error during translating statement - Wrong elements count for statement");
            }

            if (!(values[values.Count - 1] is ParseTreeNodeTerminalTranslationValue tv && tv.Token.Is(SpiceTokenType.NEWLINE)))
            {
                throw new ParseTreeEvaluationException("Error during translating statement - Statement is not finished by newline");
            }

            if (values.Count == 3 && values[1] is ParseTreeNonTerminalEvaluationValue nv && nv.SpiceObject != null && !(nv.SpiceObject is CommentLine c))
            {
                throw new ParseTreeEvaluationException("Error during translating statement - Statement has second element that is not comment");
            }

            var statement = values.GetSpiceObject<Statement>(0);

            return statement;
        }

        /// <summary>
        /// Returns new instance of <see cref="Model"/>
        /// from the values of children nodes of <see cref="Symbols.MODEL"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="Model"/>
        /// </returns>
        private SpiceObject CreateModel(ParseTreeNodeEvaluationValues values)
        {
            var model = new Model.Spice.Objects.Model();
            model.Name = values.GetLexem(2);
            model.Parameters = values.GetSpiceObject<ParameterCollection>(3);
            model.LineNumber = values.GetLexemLineNumber(2);
            return model;
        }

        /// <summary>
        /// Returns new instance of <see cref="VectorParameter"/>
        /// from the values of children nodes of <see cref="Symbols.VECTOR"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="VectorParameter"/>
        /// </returns>
        private SpiceObject CreateVector(ParseTreeNodeEvaluationValues values)
        {
            var vector = new VectorParameter();

            if (values.Count == 4)
            {
                vector.Elements.Add(values.GetSpiceObject<SingleParameter>(0));
                vector.Elements.Add(values.GetSpiceObject<SingleParameter>(2));
                vector.Elements.AddRange(values.GetSpiceObject<VectorParameter>(3).Elements);
            }

            return vector;
        }

        /// <summary>
        /// Returns new instance of <see cref="VectorParameter"/>
        /// from the values of children nodes of <see cref="Symbols.VECTOR_CONTINUE"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="VectorParameter"/>
        /// </returns>
        private SpiceObject CreateVectorContinue(ParseTreeNodeEvaluationValues values)
        {
            var vector = new VectorParameter();

            if (values.Count == 3)
            {
                vector.Elements.Add(values.GetSpiceObject<SingleParameter>(1));
                vector.Elements.AddRange(values.GetSpiceObject<VectorParameter>(2).Elements);
            }

            return vector;
        }

        /// <summary>
        /// Returns new instance of <see cref="BracketParameter"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER_BRACKET"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="BracketParameter"/>
        /// </returns>
        private SpiceObject CreateBracketParameter(ParseTreeNodeEvaluationValues values)
        {
            var parameter = new BracketParameter();
            if (values.Count == 4)
            {
                parameter.Name = values.GetLexem(0);
                parameter.Parameters = values.GetSpiceObject<ParameterCollection>(2);
            }
            else
            {
                throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
            }

            return parameter;
        }

        /// <summary>
        /// Returns new instance of <see cref="ParameterCollection"/>
        /// from the values of children nodes of <see cref="Symbols.BRACKET_CONTENT"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="ParameterCollection"/>
        /// </returns>
        private SpiceObject CreateBracketParameterContent(ParseTreeNodeEvaluationValues values)
        {
            var parameters = new ParameterCollection();

            if (values.Count == 0)
            {
                return parameters;
            }

            if (values.Count == 1 && values.TryToGetSpiceObject(0, out ParameterCollection pc))
            {
                parameters.Merge(pc);
            }
            else
            {
                throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
            }

            return parameters;
        }

        /// <summary>
        /// Returns new instance of <see cref="ParameterCollection"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER_EQUAL_SEQUANCE_CONTINUE"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="ParameterCollection"/>
        /// </returns>
        private SpiceObject CreateAssigmentParametersContinue(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count == 2)
            {
                var parameters = new ParameterCollection();

                if (values.TryToGetSpiceObject(0, out AssignmentParameter ap))
                {
                    parameters.Add(ap);
                }

                if (values.TryToGetSpiceObject(1, out ParameterCollection p))
                {
                    parameters.Merge(p);
                }

                return parameters;
            }
            else
            {
                if (values.Count != 0)
                {
                    throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
                }

                return new ParameterCollection();
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="ParameterCollection"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER_EQUAL_SEQUANCE"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="ParameterCollection"/>
        /// </returns>
        private SpiceObject CreateAssigmentParameters(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count == 2)
            {
                var parameters = new ParameterCollection();

                if (values.TryToGetSpiceObject(0, out AssignmentParameter ap))
                {
                    parameters.Add(ap);
                }

                if (values.TryToGetSpiceObject(1, out ParameterCollection p))
                {
                    parameters.Merge(p);
                }

                return parameters;
            }
            else
            {
                throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="ParameterCollection"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER_SINGLE_SEQUENCE"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="ParameterCollection"/>
        /// </returns>
        private SpiceObject CreateSingleParametersContinue(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count == 2)
            {
                var parameters = new ParameterCollection();

                if (values.TryToGetSpiceObject(0, out SingleParameter sp))
                {
                    parameters.Add(sp);
                }

                if (values.TryToGetSpiceObject(1, out ParameterCollection p))
                {
                    parameters.Merge(p);
                }

                return parameters;
            }
            else
            {
                if (values.Count != 0)
                {
                    throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
                }

                return new ParameterCollection();
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="ParameterCollection"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER_EQUAL_SEQUANCE"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="ParameterCollection"/>
        /// </returns>
        private SpiceObject CreateSingleParameters(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count == 2)
            {
                var parameters = new ParameterCollection();

                if (values.TryToGetSpiceObject(0, out SingleParameter sp))
                {
                    parameters.Add(sp);
                }

                if (values.TryToGetSpiceObject(1, out ParameterCollection p))
                {
                    parameters.Merge(p);
                }

                return parameters;
            }
            else
            {
                throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="AssignmentParameter"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER_EQUAL_SINGLE"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="AssignmentParameter"/>
        /// </returns>
        private SpiceObject CreateAssigmentSimpleParameter(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count == 3)
            {
                var assigmentParameter = new AssignmentParameter();
                assigmentParameter.Name = values.GetLexem(0);
                var singleParameter = values.GetSpiceObject<SingleParameter>(2);
                assigmentParameter.Value = singleParameter.Image;
                return assigmentParameter;
            }
            else
            {
                throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="AssignmentParameter"/>
        /// from the values of children nodes of <see cref="Symbols.PARAMETER_EQUAL"/> parse tree node
        /// </summary>
        /// <returns>
        /// A instance of <see cref="AssignmentParameter"/>
        /// </returns>
        private SpiceObject CreateAssigmentParameter(ParseTreeNodeEvaluationValues values)
        {
            if (values.Count == 1)
            {
                return values.GetSpiceObject<AssignmentParameter>(0);
            }
            else
            {
                var assigmentParameter = new AssignmentParameter();
                assigmentParameter.Name = values.GetLexem(0);

                if (values.Count == 6)
                {
                    var arguments = values.GetSpiceObject<SpiceObject>(2);
                    if (arguments is VectorParameter vp)
                    {
                        foreach (SingleParameter parameter in vp.Elements)
                        {
                            assigmentParameter.Arguments.Add(parameter.Image);
                        }
                    }
                    else
                    {
                        assigmentParameter.Arguments.Add(values.GetSpiceObject<SingleParameter>(2).Image);
                    }

                    var valueParameter = values.GetSpiceObject<SingleParameter>(5);
                    assigmentParameter.Value = valueParameter.Image;
                    return assigmentParameter;
                }

                throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
            }
        }

        /// <summary>
        /// Returns new instance of <see cref="Statements"/>
        /// from the values of children nodes of <see cref="Symbols.STATEMENTS"/> parse tree node
        /// </summary>
        /// <returns>
        /// A new instance of <see cref="Statements"/>
        /// </returns>
        private SpiceObject CreateStatements(ParseTreeNodeEvaluationValues values)
        {
            var statements = new Statements();

            if (values.Count == 2)
            {
                if (values.TryToGetSpiceObject<Statement>(0, out Statement st))
                {
                    statements.Add(st);
                }

                statements.Merge(values.GetSpiceObject<Statements>(1));
            }
            else
            {
                if (values.Count == 1)
                {
                    if (values.TryToGetSpiceObject(0, out Statements sts))
                    {
                        statements.Merge(sts);
                    }
                    else
                    {
                        if (values.TryToGetToken(0, out SpiceToken token) && token.Is(SpiceTokenType.END))
                        {
                            // skip
                        }
                        else
                        {
                            throw new ParseTreeEvaluationException("Error during translating parse tree to Spice Object Model");
                        }
                    }
                }
            }

            return statements;
        }
    }
}