using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Eval.Csharp
{
    class ExpressionTransformer
    {
        private Dictionary<string, object> _variables;
        private ExpressionStatementSyntax _expr;
        private ExecutionContext _context;

        public ExpressionTransformer(ExpressionStatementSyntax expr) : this(expr, new Dictionary<string, object>(), null) { }

        public ExpressionTransformer(ExpressionStatementSyntax expr, Dictionary<string, object> variables, ExecutionContext context)
        {
            _expr = expr;
            _variables = variables;
            _context = context;
        }

        private ConstantExpression TransformLiteralExpression(LiteralExpressionSyntax node)
        {
            string literal = node.GetText().ToString();
            switch (node.Kind())
            {
                case SyntaxKind.NumericLiteralExpression:
                    return Expression.Constant(int.Parse(literal), typeof(int));
                case SyntaxKind.StringLiteralExpression:
                    return Expression.Constant(literal.TrimStart('"').TrimEnd('"'), typeof(string));
                case SyntaxKind.CharacterLiteralExpression:
                    return Expression.Constant(literal.TrimStart('\'').TrimEnd('\'')[0], typeof(char));
                case SyntaxKind.TrueLiteralExpression:
                    return Expression.Constant(true, typeof(bool));
                case SyntaxKind.FalseLiteralExpression:
                    return Expression.Constant(true, typeof(bool));
                case SyntaxKind.NullLiteralExpression:
                    return Expression.Constant(null);
                default:
                    return null;
            }
        }

        private BinaryExpression TransformBinaryExpressionSyntax(BinaryExpressionSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.AddExpression:
                    return Expression.Add(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                case SyntaxKind.SubtractExpression:
                    return Expression.Subtract(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                case SyntaxKind.MultiplyExpression:
                    return Expression.Multiply(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                case SyntaxKind.DivideExpression:
                    return Expression.Divide(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                default:
                    return null;
            }
        }

        private BinaryExpression TransformAssignmentExpressionSyntax(AssignmentExpressionSyntax node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.AddAssignmentExpression:
                    return Expression.AddAssign(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                case SyntaxKind.SubtractAssignmentExpression:
                    return Expression.SubtractAssign(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                case SyntaxKind.MultiplyAssignmentExpression:
                    return Expression.MultiplyAssign(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                case SyntaxKind.DivideAssignmentExpression:
                    return Expression.DivideAssign(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                case SyntaxKind.ModuloAssignmentExpression:
                    return Expression.ModuloAssign(TransformExpressionSyntax(node.Left), TransformExpressionSyntax(node.Right));
                default:
                    return null;
            }
        }

        private ConstantExpression TransformIdentifierNameSyntax(IdentifierNameSyntax node)
        {
            string identifier = node.Identifier.ValueText;
            object @value = null;

            if (identifier == "this")
                @value = this._context.Value;

            if (@value == null)
                _variables.TryGetValue(identifier, out @value);

            if (@value == null)
                throw new Exception(string.Format("CS0103: The name '{0}' does not exist in the current context", identifier));

            return Expression.Constant(@value, @value.GetType());
        }

        private ConstantExpression TransformThisExpressionSyntax(ThisExpressionSyntax node)
        {
            return Expression.Constant(this._context.Value, this._context.Type);
        }

        private MemberExpression TransformMemberAccessExpressionSyntax(MemberAccessExpressionSyntax node)
        {
            return Expression.PropertyOrField(TransformExpressionSyntax(node.Expression), node.Name.Identifier.ValueText);
        }

        private MethodCallExpression TransformInvocationExpressionSyntax(InvocationExpressionSyntax node)
        {
            var suppliedArgs = node.ArgumentList.Arguments;
            int argsLength = suppliedArgs.Count();
            Expression[] arguments = new Expression[argsLength];

            for (int i = 0; i < argsLength; i++)
                arguments[i] = TransformExpressionSyntax(suppliedArgs[i].Expression);

            ExpressionSyntax invoker = node.Expression;
            if (invoker.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                string methodName = ((MemberAccessExpressionSyntax)invoker).Name.Identifier.ValueText;
                var instance = TransformExpressionSyntax(((MemberAccessExpressionSyntax)invoker).Expression);
                return Expression.Call(instance, methodName, null, arguments);
            }
            else if (invoker.Kind() == SyntaxKind.IdentifierName)
            {
                string methodName = ((IdentifierNameSyntax)invoker).Identifier.ValueText;
                if (this._context == null)
                    throw new Exception(string.Format("CS0103: The name '{0}' does not exist in the current context", methodName));
                else if (!this._context.IsStatic)
                    throw new Exception(string.Format("CS0103: The name '{0}' does not exist in the current context", methodName));

                return Expression.Call(this._context.Type, methodName, null, arguments);
            }

            throw new Exception("Unsupported Expression");
        }

        private Expression TransformExpressionSyntax(ExpressionSyntax node)
        {
            switch (node.GetType().ToString())
            {
                case "Microsoft.CodeAnalysis.CSharp.Syntax.LiteralExpressionSyntax":
                    return TransformLiteralExpression((LiteralExpressionSyntax)node);
                case "Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax":
                    return TransformBinaryExpressionSyntax((BinaryExpressionSyntax)node);
                case "Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax":
                    return TransformIdentifierNameSyntax((IdentifierNameSyntax)node);
                case "Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax":
                    return TransformMemberAccessExpressionSyntax((MemberAccessExpressionSyntax)node);
                case "Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax":
                    return TransformInvocationExpressionSyntax((InvocationExpressionSyntax)node);
                case "Microsoft.CodeAnalysis.CSharp.Syntax.ThisExpressionSyntax":
                    return TransformThisExpressionSyntax((ThisExpressionSyntax)node);
                case "Microsoft.CodeAnalysis.CSharp.Syntax.AssignmentExpressionSyntax":
                    return TransformAssignmentExpressionSyntax((AssignmentExpressionSyntax)node);
                default:
                    throw new Exception("Unsupported Expression");
            }
        }

        public Expression Transform()
        {
            return TransformExpressionSyntax(_expr.ChildNodes().OfType<ExpressionSyntax>().First());
        }
    }
}
