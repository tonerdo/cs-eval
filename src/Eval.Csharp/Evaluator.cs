using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eval.Csharp
{
    public class Evaluator
    {
        private string _code;
        private Dictionary<string, object> _variables;
        private object _context;
        private List<Type> _exportedTypes;

        public Evaluator(string code)
        {
            this._code = code;
            this._variables = new Dictionary<string, object>();
            this._exportedTypes = new List<Type>();
        }

        public Evaluator AddVariable<T>(string identifier, T obj)
        {
            this._variables.Add(identifier, obj);
            return this;
        }

        public Evaluator SetContext(object obj)
        {
            if (obj == null)
                throw new Exception("Context object cannot be null");

            this._context = obj;
            return this;
        }

        public Evaluator AddTypes(params Type[] types)
        {
            this._exportedTypes.AddRange(types);
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

        private ExpressionTransformer CreateExpressionTransformer()
        {
            ExpressionStatementSyntax expr = ValidateCode().GetRoot()
                .DescendantNodes()
                .OfType<ExpressionStatementSyntax>()
                .First();

            ExpressionTransformer transformer = new ExpressionTransformer(expr, _variables, _context, _exportedTypes);
            return transformer;
        }

        private T EvaluateImpl<T>()
        {
            var transformer = CreateExpressionTransformer();
            var expression = Expression.Lambda<T>(transformer.Transform());
            return expression.Compile();
        }

        public Func<T> Evaluate<T>() => EvaluateImpl<Func<T>>();

        public Action Evaluate() => EvaluateImpl<Action>();
    }
}
