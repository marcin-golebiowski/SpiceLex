﻿using SpiceSharpParser.Models.Netlist.Spice.Objects;

namespace SpiceSharpParser.ModelReaders.Netlist.Spice.Readers.Controls
{
    using SpiceSharpParser.Common.Evaluation;

    /// <summary>
    /// Reads .PARAM <see cref="Control"/> from SPICE netlist object model.
    /// </summary>
    public class ParamControl : ParamBaseControl
    {
        protected override void SetParameter(string parameterName, string parameterExpression, IExpressionParser expressionParser, ExpressionContext expressionContext, SpiceNetlistCaseSensitivitySettings caseSettings)
        {
            expressionContext.SetParameter(
                            parameterName,
                            parameterExpression,
                            expressionParser.Parse(
                                parameterExpression,
                                new ExpressionParserContext(caseSettings.IsFunctionNameCaseSensitive)
                                {
                                    Functions = expressionContext.Functions
                                }).FoundParameters);
        }
    }
}
