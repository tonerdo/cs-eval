using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eval.Csharp
{
    public class Eval
    {
        private string _code;
        private Dictionary<string, object> _variables;

        public Eval(string code)
        {
            this._code = code;
            this._variables = new Dictionary<string, object>();
        }

        public Eval AddVariable<T>(string identifier, T @object)
        {
            this._variables.Add(identifier, @object);
            return this;
        }

        private SyntaxTree ValidateCode()
        {
            if (string.IsNullOrEmpty(_code) || string.IsNullOrWhiteSpace(_code))
                throw new Exception("Code cannot be empty or null");

            var tree = CSharpSyntaxTree.ParseText(_code, CSharpParseOptions.Default.WithKind(SourceCodeKind.Script));
            var diagnostics = tree.GetDiagnostics().ToList();

            if (diagnostics.Count > 0)
                throw new Exception(diagnostics[0].ToString());

            return tree;
        }

        private ExpressionTransformer GetExpressionTransformer()
        {
            ExpressionStatementSyntax expr = ValidateCode().GetRoot()
                .DescendantNodes()
                .OfType<ExpressionStatementSyntax>()
                .First();

            ExpressionTransformer transformer = new ExpressionTransformer(expr, _variables);
            return transformer;
        }

        private T EvaluateImpl<T>()
        {
            var transformer = GetExpressionTransformer();
            var expression = Expression.Lambda<T>(transformer.Transform());
            return expression.Compile();
        }

        public Func<T> Evaluate<T>() => EvaluateImpl<Func<T>>();

        public Action Evaluate() => EvaluateImpl<Action>();
    }
}
