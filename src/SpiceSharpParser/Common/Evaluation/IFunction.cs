﻿using System;
using SpiceSharpBehavioral.Parsers;

namespace SpiceSharpParser.Common.Evaluation
{
    public interface IFunction<in TInputArgumentType, out TOutputType> : IFunction
    {
        TOutputType Logic(string image, TInputArgumentType[] args, EvaluationContext context);
    }

    public interface IDerivativeFunction<TInputArgumentType, TOutputType> : IFunction<TInputArgumentType, TOutputType>
    {
        Derivatives<Func<TOutputType>> Derivative(string image, Func<TInputArgumentType>[] args, EvaluationContext context);
    }

    public interface IFunction
    {
        int ArgumentsCount { get; set; }

        string Name { get; set; }
    }
}