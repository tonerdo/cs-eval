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
        private ExecutionContext _context;

        public Evaluator(string code)
        {
            this._code = code;
            this._variables = new Dictionary<string, object>();
        }

        public Evaluator AddVariable<T>(string identifier, T @object)
        {
            this._variables.Add(identifier, @object);
            return this;
        }

        public Evaluator SetContext(Type type)
        {
            if (type == null)
                throw new Exception("Static context object cannot be null");

            this._context = new ExecutionContext { Type = type, Value = null, IsStatic = true };
            return this;
        }

        public Evaluator SetContext(object @object)
        {
            if (@object == null)
                throw new Exception("Non-static context object cannot be null");

            this._context = new ExecutionContext { Type = @object.GetType(), Value = @object, IsStatic = false };
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

            ExpressionTransformer transformer = new ExpressionTransformer(expr, _variables, _context);
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
