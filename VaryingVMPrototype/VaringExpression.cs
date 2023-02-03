namespace VaryingFromExpression;

using System.Linq.Expressions;

class ExpressionVaryingSyntaxVisitor : ExpressionVisitor
{
    public IVaryingSyntax? Result;

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Type == typeof(float) && node.Value is float v)
        {
            Result = Varying.Lit(v);
        }

        return base.VisitConstant(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Result = Varying.Symbol;
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node.Parameters.Count != 1 || node.Parameters[0].Type != typeof(float))
        {
            throw new NotSupportedException();
        }

        Visit(node.Body);
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var e = node.Expression;
        if (e is ConstantExpression ce)
        {
            Result = Varying.Lit((float)ce.Value.GetType().GetField(node.Member.Name).GetValue(ce.Value));
        }
        else
        {
            Visit(node.Expression);
        }

        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        Visit(node.Left);
        var l = Result;
        if (l is null)
        {
            throw new Exception();
        }

        Result = null;

        Visit(node.Right);
        var r = Result;
        if (r is null)
        {
            throw new Exception();
        }

        Result = null;

        switch (node.NodeType)
        {
            case ExpressionType.Multiply:
                Result = Varying.Multiply(l, r);
                return node;
            case ExpressionType.Add:
                Result = Varying.Add(l, r);
                return node;
            case ExpressionType.Subtract:
                Result = Varying.Add(l, Varying.Multiply(Varying.Lit(-1.0f), r));
                return node;
            default:
                return node;
        }
    }
}

static partial class Varying
{
    public static IVaryingSyntax ToVaryingSyntax(this Expression<Func<float, float>> code)
    {
        var visitor = new ExpressionVaryingSyntaxVisitor();
        visitor.Visit(code);
        if (visitor.Result is null)
        {
            throw new Exception();
        }
        else
        {
            return visitor.Result;
        }
    }

    static readonly ParameterExpression k_ParameterTExpression = Expression.Parameter(typeof(float), "t");

    static readonly IVaryingSemantic<Expression> ExpressionSemantic = new FreeVaryingSemantic<Expression>(
        static (_, _) => k_ParameterTExpression,
        static (_, _) =>
        {
            Expression<Func<float>> e = () => new Random().NextSingle();
            return e.Body;
        },
        static (_, _, value) => Expression.Constant(value, typeof(float)),
        static (_, _, l, r) => Expression.Add(l, r),
        static (_, _, l, r) => Expression.Multiply(l, r),
        static (_, _, x, y, s) =>
            Expression.Add(
                Expression.Multiply(
                    Expression.Subtract(Expression.Constant(1.0f, typeof(float)), s), x),
                Expression.Multiply(
                    s, y
                ))
    );

    public static IVaryingSyntax FromExpression(Expression<Func<float, float>> e) => e.ToVaryingSyntax();

    public static Expression<Func<float, float>> ToExpression(this IVaryingSyntax code)
        => Expression.Lambda<Func<float, float>>(
            code.Evaluate(ExpressionSemantic),
            new[] { k_ParameterTExpression }
        );
}
