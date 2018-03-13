﻿using System.Collections.Generic;

namespace SpiceNetlist.SpiceSharpConnector.Processors.Evaluation
{
    /// <summary>
    /// An interface for all evaluators
    /// </summary>
    public interface IEvaluator
    {
        /// <summary>
        /// Adds double expression to registry that will be updated when value of parameter change
        /// </summary>
        /// <param name="expression">An expression to add</param>
        void AddDynamicExpression(DoubleExpression expression);

        /// <summary>
        /// Evalues a specific string to double
        /// </summary>
        /// <param name="expression">An expression to evaluate</param>
        /// <returns>
        /// A double value
        /// </returns>
        double EvaluateDouble(string expression);

        /// <summary>
        /// Sets the parameter value and updates the values expressions
        /// </summary>
        /// <param name="parameterName">A name of parameter</param>
        /// <param name="value">A value of parameter</param>
        void SetParameter(string parameterName, double value);

        /// <summary>
        /// Sets the parameter value and updates the values expressions
        /// </summary>
        /// <param name="parameterName">A name of parameter</param>
        /// <param name="expression">A parameter expression </param>
        void SetParameter(string parameterName, string expression);

        /// <summary>
        /// Returns a value indicating whether there is a parameter in evaluator with given name
        /// </summary>
        /// <param name="parameterName">A parameter name</param>
        /// <returns>
        /// True if there is parameter
        /// </returns>
        bool HasParameter(string parameterName);

        /// <summary>
        /// Gets the value of parameter
        /// </summary>
        /// <param name="parameterName">A parameter name</param>
        /// <returns>
        /// A value of parameter
        /// </returns>
        double GetParameterValue(string parameterName);

        /// <summary>
        /// Gets the names of parameters
        /// </summary>
        /// <returns>
        /// The names of paramaters
        /// </returns>
        IEnumerable<string> GetParameterNames();
    }
}
