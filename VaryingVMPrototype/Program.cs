using System.Linq.Expressions;

namespace VaryingFromExpression;

interface IAlgebra<TR>
{
    TR Symbol();
    TR Lit(float value);
    TR Multiply(TR left, TR right);
    TR Add(TR left, TR right);
}

interface ISyntax
{
    TR Evaluate<TR>(IAlgebra<TR> algebra);
}

record struct SymbolFreeSyntax : ISyntax
{
    public TR Evaluate<TR>(IAlgebra<TR> algebra) => algebra.Symbol();
}

record struct LitFreeSyntax(float Value) : ISyntax
{
    public TR Evaluate<TR>(IAlgebra<TR> algebra) => algebra.Lit(Value);
}

record struct MultipleSyntax(ISyntax Left, ISyntax Right) : ISyntax
{
    public TR Evaluate<TR>(IAlgebra<TR> algebra) => algebra.Multiply(Left.Evaluate(algebra), Right.Evaluate(algebra));
}

record struct AddFreeSyntax(ISyntax Left, ISyntax Right) : ISyntax
{
    public TR Evaluate<TR>(IAlgebra<TR> algebra) => algebra.Add(Left.Evaluate(algebra), Right.Evaluate(algebra));
}

struct FreeSyntaxAlgebra : IAlgebra<ISyntax>
{
    public ISyntax Symbol()
        => new SymbolFreeSyntax();

    public ISyntax Lit(float value)
        => new LitFreeSyntax(value);

    public ISyntax Multiply(ISyntax left, ISyntax right)
        => new MultipleSyntax(left, right);

    public ISyntax Add(ISyntax left, ISyntax right)
        => new AddFreeSyntax(left, right);
}

class InterpretAlgebra : IAlgebra<Func<float, float>>
{
    public Func<float, float> Symbol() => t => t;
    public Func<float, float> Lit(float value) => t => value;

    public Func<float, float> Multiply(Func<float, float> left, Func<float, float> right) =>
        t => left(t) * right(t);

    public Func<float, float> Add(Func<float, float> left, Func<float, float> right) => t => left(t) + right(t);
}

class PolynomialAlgebra : IAlgebra<IVarying<float>>
{
    public IVarying<float> Symbol() => Varying.T();
    public IVarying<float> Lit(float value) => Varying.Polynomial(value);

    public IVarying<float> Multiply(IVarying<float> left, IVarying<float> right)
        => (left, right) switch
        {
            (Polynomial0 pl, Polynomial0 pr) => Varying.Mul(pl, pr),
            (Polynomial0 pl, Polynomial1 pr) => Varying.Mul(pl, pr),
            (Polynomial0 pl, Polynomial2 pr) => Varying.Mul(pl, pr),
            (Polynomial1 pl, Polynomial0 pr) => Varying.Mul(pr, pl),
            (Polynomial1 pl, Polynomial1 pr) => Varying.Mul(pl, pr),
            (Polynomial1 pl, Polynomial2 pr) => Varying.Mul(pl, pr),
            (Polynomial2 pl, Polynomial0 pr) => Varying.Mul(pr, pl),
            (Polynomial2 pl, Polynomial1 pr) => Varying.Mul(pr, pl),
            _ => throw new NotImplementedException()
        };


    public IVarying<float> Add(IVarying<float> left, IVarying<float> right)
        => (left, right) switch
        {
            (Polynomial0 pl, Polynomial0 pr) => Varying.Add(pl, pr),
            (Polynomial0 pl, Polynomial1 pr) => Varying.Add(pl, pr),
            (Polynomial0 pl, Polynomial2 pr) => Varying.Add(pl, pr),
            (Polynomial1 pl, Polynomial0 pr) => Varying.Add(pr, pl),
            (Polynomial1 pl, Polynomial1 pr) => Varying.Add(pl, pr),
            (Polynomial1 pl, Polynomial2 pr) => Varying.Add(pl, pr),
            (Polynomial2 pl, Polynomial0 pr) => Varying.Add(pr, pl),
            (Polynomial2 pl, Polynomial1 pr) => Varying.Add(pr, pl),
            (Polynomial2 pl, Polynomial2 pr) => Varying.Add(pl, pr),
            _ => throw new NotImplementedException()
        };
}

record struct PolynomialAsSyntax(IVarying<float> Polynomial) : ISyntax
{
    record struct Impl : IVarying<float>.IPattern<ISyntax>
    {
        public ISyntax Polynomial0(in Polynomial0 p)
            => new LitFreeSyntax(p.Value);

        public ISyntax Polynomial1(in Polynomial1 p)
        {
            if (p.Coefficients.X == 0.0f && Math.Abs(p.Coefficients.Y - 1.0f) < 1e-5f)
            {
                return new SymbolFreeSyntax();
            }
            else
            {
                var a1 = p.Coefficients.Y;
                var a0 = p.Coefficients.X;
                return Extensions.FromExpression(t => a1 * t + a0);
            }
        }

        public ISyntax Polynomial2(in Polynomial2 p)
        {
            var a2 = p.Coefficients.Z;
            var a1 = p.Coefficients.Y;
            var a0 = p.Coefficients.X;
            return Extensions.FromExpression(t => a2 * t * t + a1 * t + a0);
        }

        public ISyntax Polynomial3(in Polynomial3 p)
        {
            throw new NotImplementedException();
        }
    }

    public TR Evaluate<TR>(IAlgebra<TR> algebra)
        => algebra.Compile(Polynomial.Match(new Impl()));
}

struct PrintCode : IAlgebra<string>
{
    public string Symbol()
        => "t";

    public string Lit(float value)
        => value.ToString();

    public string Multiply(string left, string right)
        => $"({left} * {right})";

    public string Add(string left, string right)
        => $"({left} + {right})";
}

class PureExpressionVisitor<TR> : ExpressionVisitor
{
    public TR? Result;
    public IAlgebra<TR> Visitor { get; init; }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Type == typeof(float) && node.Value is float v)
        {
            Result = Visitor.Lit(v);
        }

        return base.VisitConstant(node);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        Result = Visitor.Symbol();
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        if (node.Parameters.Count != 1 || node.Parameters[0].Type != typeof(float))
        {
            throw new NotImplementedException();
        }

        Visit(node.Body);
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var e = node.Expression;
        if (e is ConstantExpression ce)
        {
            Result = Visitor.Lit((float)ce.Value.GetType().GetField(node.Member.Name).GetValue(ce.Value));
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

        Visit(node.Right);
        var r = Result;
        if (r is null)
        {
            throw new Exception();
        }

        if (node.NodeType == ExpressionType.Multiply)
        {
            Result = Visitor.Multiply(l, r);
            return node;
        }

        if (node.NodeType == ExpressionType.Add)
        {
            Result = Visitor.Add(l, r);
            return node;
        }


        return node;
    }
}

record struct SubstituteAlgebra(ISyntax NewSymbol) : IAlgebra<ISyntax>
{
    public ISyntax Symbol() => NewSymbol;


    public ISyntax Lit(float value) => new LitFreeSyntax(value);


    public ISyntax Multiply(ISyntax left, ISyntax right) => new MultipleSyntax(left, right);


    public ISyntax Add(ISyntax left, ISyntax right)
        => new AddFreeSyntax(left, right);
}

static class Extensions
{
    public static TR Compile<TR>(this IAlgebra<TR> compiler, Expression<Func<float, float>> code)
    {
        var visitor = new PureExpressionVisitor<TR>()
        {
            Visitor = compiler
        };
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

    public static TR Compile<TR>(this IAlgebra<TR> compiler, ISyntax code) => code.Evaluate(compiler);

    public static ISyntax Substitute(this ISyntax code, ISyntax symbol)
        => code.Evaluate(new SubstituteAlgebra(symbol));

    public static ISyntax FromExpression(Expression<Func<float, float>> e) => new FreeSyntaxAlgebra().Compile(e);

    public static IVarying<float> ToPolynomial(this ISyntax code) => new PolynomialAlgebra().Compile(code);
    public static ISyntax ToCode(this IVarying<float> code) => new PolynomialAsSyntax(code);
}

record struct ReportTestCase(Expression<Func<float, float>> Expr)
{
    public void Report(float testT)
    {
        var cSharpInterpreter = new InterpretAlgebra();
        var polynomial = new PolynomialAlgebra();
        var p = polynomial.Compile(Expr);
        var vInHouse = cSharpInterpreter.Compile(Expr)(testT);
        var vReference = Expr.Compile()(testT);
        var freeCode = new FreeSyntaxAlgebra().Compile(Expr);
        Console.WriteLine($"Expr<{Expr}>");
        Console.WriteLine($"Poly<{p}>");
        Console.WriteLine($"Value({testT})<Reference={vReference},InHouse={vInHouse}>");
        Console.WriteLine($"FreeCode<{freeCode}>");
        Console.WriteLine("---");
    }
}

static internal class Program
{
    static void Main(string[] args)
    {
        Expression<Func<float, float>> tPlusOneSquare = t => t * t + 2.0f * t + 1.0f;
        Expression<Func<float, float>> tPlusOne = t => t + 1.0f;
        Expression<Func<float, float>> tSquare = t => t * t;
        var toSyntax = new FreeSyntaxAlgebra();

        var testCases = new Expression<Func<float, float>>[]
        {
            t => 42,
            t => t,
            t => 3.0f * t,
            t => 1.0f + t + 2.0f * t,
            tPlusOneSquare,
            tPlusOne,
        };


        foreach (var tc in testCases)
        {
            new ReportTestCase(tc).Report(1.0f);
        }

        var tSquareCode = toSyntax.Compile(tSquare);
        var tPlusOneCode = toSyntax.Compile(tPlusOne);
        var tPlusOneSquareComposite = tSquareCode.Substitute(tPlusOneCode);
        var tPlusOnePolynomial = new PolynomialAlgebra().Compile(tPlusOneSquareComposite);
        Console.WriteLine($"Composite result: {tPlusOnePolynomial}");

        Console.WriteLine(toSyntax.Compile(new PolynomialAsSyntax(new Polynomial0(42.0f))));
        Console.WriteLine(toSyntax.Compile(tPlusOnePolynomial.ToCode()));
        Console.WriteLine(new PrintCode().Compile(toSyntax.Compile(tPlusOnePolynomial.ToCode())));
    }
}