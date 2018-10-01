﻿using System;
using System.Collections.Generic;
using SpiceSharpParser.Common.Evaluation.Expressions;
using SpiceSharpParser.Common.Evaluation.Functions;

namespace SpiceSharpParser.Common.Evaluation
{
    /// <summary>
    /// Abstract evaluator.
    /// </summary>
    public abstract class Evaluator : IEvaluator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Evaluator"/> class.
        /// </summary>
        /// <param name="name">Evaluator name.</param>
        /// <param name="context">Evaluator context.</param>
        /// <param name="parser">Expression parser.</param>
        /// <param name="registry">Expression registry.</param>
        /// <param name="seed">Random seed.</param>
        /// <param name="isFunctionNameCaseSensitive">Is function name case-sensitive.</param>
        /// <param name="isParameterNameCaseSensitive">Is parameter name case-sensitive.</param>
        public Evaluator(string name, object context, IExpressionParser parser, ExpressionRegistry registry, int? seed, bool isFunctionNameCaseSensitive, bool isParameterNameCaseSensitive)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Context = context;
            ExpressionParser = parser ?? throw new ArgumentNullException(nameof(parser));
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Seed = seed;
            IsFunctionNameCaseSensitive = isFunctionNameCaseSensitive;
            IsParameterNameCaseSensitive = isParameterNameCaseSensitive;
            Parameters = new Dictionary<string, Expression>(StringComparerFactory.Create(isParameterNameCaseSensitive));
            Functions = new Dictionary<string, Function>(StringComparerFactory.Create(isFunctionNameCaseSensitive));
            CreateCommonFunctions();
        }

        /// <summary>
        /// Gets or sets the name of the evaluator.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public Dictionary<string, Expression> Parameters { get; protected set; }

        /// <summary>
        /// Gets or sets custom functions.
        /// </summary>
        public Dictionary<string, Function> Functions { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether function names are case-sensitive.
        /// </summary>
        public bool IsFunctionNameCaseSensitive { get; }

        /// <summary>
        /// Gets a value indicating whether parameter names are case-sensitive.
        /// </summary>
        public bool IsParameterNameCaseSensitive { get; }

        /// <summary>
        /// Gets the children evaluators.
        /// </summary>
        public List<IEvaluator> Children { get; } = new List<IEvaluator>();

        /// <summary>
        /// Gets or sets the random seed for the evaluator.
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// Gets or sets the context of the evaluator.
        /// </summary>
        public object Context { get; set; }

        /// <summary>
        /// Gets the expression registry.
        /// </summary>
        protected ExpressionRegistry Registry { get; }

        /// <summary>
        /// Gets the expression parser.
        /// </summary>
        protected IExpressionParser ExpressionParser { get; private set; }

        /// <summary>
        /// Evaluates a specific expression to double.
        /// </summary>
        /// <param name="expression">An expression to evaluate.</param>
        /// <returns>
        /// A double value.
        /// </returns>
        public double EvaluateDouble(string expression)
        {
            if (Parameters.ContainsKey(expression))
            {
                return Parameters[expression].Evaluate();
            }

            var parseResult = ExpressionParser.Parse(
                expression,
                new ExpressionParserContext() { Functions = Functions, Parameters = Parameters, Evaluator = this });

            return parseResult.Value();
        }

        /// <summary>
        /// Gets the value of parameter.
        /// </summary>
        /// <param name="id">A parameter identifier.</param>
        /// <returns>
        /// A value of parameter.
        /// </returns>
        public double GetParameterValue(string id)
        {
            return Parameters[id].Evaluate();
        }

        /// <summary>
        /// Gets the parameters from expression.
        /// </summary>
        /// <param name="expression">The expression to check.</param>
        /// <returns>
        /// Parameters from expression.
        /// </returns>
        public ICollection<string> GetParametersFromExpression(string expression)
        {
            var result = ExpressionParser.Parse(expression, new ExpressionParserContext() { Functions = Functions, Parameters = Parameters, Evaluator = this }, false);
            return result.FoundParameters;
        }

        /// <summary>
        /// Sets parameters.
        /// </summary>
        /// <param name="parameters">Parameters to set.</param>
        public void SetParameters(Dictionary<string, string> parameters)
        {
            foreach (var parameter in parameters)
            {
                Parameters[parameter.Key] = new CachedExpression(parameter.Value, this);
                Registry.UpdateParameterDependencies(parameter.Key, GetParametersFromExpression(parameter.Value));
            }

            foreach (var parameter in parameters)
            {
                RefreshForParameter(parameter.Key);
            }
        }

        /// <summary>
        /// Sets the parameter.
        /// </summary>
        /// <param name="id">A name of parameter.</param>
        /// <param name="value">A value of parameter.</param>
        public void SetParameter(string id, double value)
        {
            Parameters[id] = new ConstantExpression(value);

            RefreshForParameter(id);

            foreach (var child in Children)
            {
                child.SetParameter(id, value);
            }
        }

        /// <summary>
        /// Sets the parameter.
        /// </summary>
        /// <param name="id">A name of parameter.</param>
        /// <param name="expression">An expression of parameter.</param>
        public void SetParameter(string id, string expression)
        {
            Parameters[id] = new CachedExpression(expression, this);

            Registry.UpdateParameterDependencies(id, GetParametersFromExpression(expression));
            RefreshForParameter(id);

            foreach (var child in Children)
            {
                child.SetParameter(id, expression);
            }
        }

        /// <summary>
        /// Gets the expression names.
        /// </summary>
        /// <returns>
        /// Enumerable of strings.
        /// </returns>
        public IEnumerable<string> GetExpressionNames()
        {
            return Registry.GetExpressionNames();
        }

        /// <summary>
        /// Gets value of named expression.
        /// </summary>
        /// <param name="expressionName">Name of expression</param>
        /// <returns>
        /// Value of expression.
        /// </returns>
        public double GetExpressionValue(string expressionName)
        {
            return Registry.GetExpression(expressionName).Evaluate();
        }

        /// <summary>
        /// Sets the named expression.
        /// </summary>
        /// <param name="expressionName">Expression name.</param>
        /// <param name="expression">Expression.</param>
        public void SetNamedExpression(string expressionName, string expression)
        {
            var parameters = GetParametersFromExpression(expression);
            Registry.Add(new NamedExpression(expressionName, expression, this), parameters);
        }

        /// <summary>
        /// Gets the expression by name.
        /// </summary>
        /// <param name="expressionName">Name of expression.</param>
        /// <returns>
        /// Expression.
        /// </returns>
        public string GetExpression(string expressionName)
        {
            return Registry.GetExpression(expressionName)?.String;
        }

        /// <summary>
        /// Creates a child evaluator.
        /// </summary>
        /// <returns>
        /// A new evaluator.
        /// </returns>
        public abstract IEvaluator CreateChildEvaluator(string name, object context);

        public abstract IEvaluator Clone(bool deep);

        public void Initialize(Dictionary<string, Expression> parameters, Dictionary<string, Function> customFunctions, List<IEvaluator> children)
        {
            foreach (var parameterName in parameters.Keys)
            {
                Parameters[parameterName] = parameters[parameterName].Clone();
                Parameters[parameterName].Evaluator = this;
                Parameters[parameterName].Invalidate();
            }

            foreach (var customFunction in customFunctions.Keys)
            {
                Functions[customFunction] = customFunctions[customFunction];
            }

            Children.Clear();
            foreach (var child in children)
            {
                Children.Add(child.Clone(true));
            }

            Registry.Invalidate(this);
        }

        /// <summary>
        /// Refreshes parameter.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        public void RefreshForParameter(string parameterName)
        {
            Registry.RefreshDependentParameters(parameterName, parameterToRefresh =>
            {
                if (parameterToRefresh != parameterName)
                {
                    Parameters[parameterToRefresh].Invalidate();
                    Parameters[parameterToRefresh].Evaluate();
                }
            });
        }

        /// <summary>
        /// Finds the child evaluator with given name.
        /// </summary>
        /// <param name="evaluatorName">Name of child evaluator to find.</param>
        /// <returns>
        /// A reference to evaluator.
        /// </returns>
        public IEvaluator FindChildEvaluator(string evaluatorName)
        {
            if (evaluatorName == Name)
            {
                return this;
            }

            foreach (var child in Children)
            {
                var result = child.FindChildEvaluator(evaluatorName);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Adds evaluator action.
        /// </summary>
        /// <param name="actionName">Action name.</param>
        /// <param name="expressionString">Expression.</param>
        /// <param name="expressionAction">Expression action.</param>
        public void AddAction(string actionName, string expressionString, Action<double> expressionAction)
        {
            if (expressionAction == null)
            {
                throw new ArgumentNullException(nameof(expressionAction));
            }

            var namedExpression = new NamedExpression(actionName, expressionString, this);
            namedExpression.Evaluated += (sender, args) => { expressionAction(args.NewValue); };

            var parameters = GetParametersFromExpression(expressionString);
            Registry.Add(namedExpression, parameters);
        }

        private void CreateCommonFunctions()
        {
            this.Functions.Add("acos", MathFunctions.CreateACos());
            this.Functions.Add("asin", MathFunctions.CreateASin());
            this.Functions.Add("atan", MathFunctions.CreateATan());
            this.Functions.Add("atan2", MathFunctions.CreateATan2());
            this.Functions.Add("cos", MathFunctions.CreateCos());
            this.Functions.Add("cosh", MathFunctions.CreateCosh());
            this.Functions.Add("sin", MathFunctions.CreateSin());
            this.Functions.Add("sinh", MathFunctions.CreateSinh());
            this.Functions.Add("tan", MathFunctions.CreateTan());
            this.Functions.Add("tanh", MathFunctions.CreateTanh());
        }
    }
}
