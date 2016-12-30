using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

class ExpressionTransformer
{
    private Dictionary<string, object> _variables;
    private ExpressionStatementSyntax _expr;

    public ExpressionTransformer(ExpressionStatementSyntax expr) : this(expr, new Dictionary<string, object>()) { }

    public ExpressionTransformer(ExpressionStatementSyntax expr, Dictionary<string, object> variables)
    {
        _expr = expr;
        _variables = variables;
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

    private ConstantExpression TransformIdentifierNameSyntax(IdentifierNameSyntax node)
    {
        string identifier = node.Identifier.ValueText;

        object @value = null;
        _variables.TryGetValue(identifier, out @value);

        if (@value == null)
            throw new Exception(string.Format("CS0103: The name '{0}' does not exist in the current context", identifier));

        return Expression.Constant(@value, @value.GetType());
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
            default:
                throw new Exception("Unsupported Expression");
        }
    }

    public Expression Transform()
    {
        return TransformExpressionSyntax(_expr.ChildNodes().OfType<ExpressionSyntax>().First());
    }
}
