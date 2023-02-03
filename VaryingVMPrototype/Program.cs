using System.Numerics;

namespace VaryingFromExpression;

using System.Linq.Expressions;

class SyntaxTestCase
{
    readonly string name;
    public readonly IVaryingSyntax Syntax;

    public SyntaxTestCase(string name, IVaryingSyntax syntax)
    {
        Syntax = syntax;
        this.name = name;
    }

    public string Name => name;
    protected virtual void PrintExpression() { }

    protected virtual void PrintTestValue(float t)
    {
        Console.WriteLine($"InHouseValue({t}) = {Syntax.ToFunc()(t)}");
        Console.WriteLine($"PolynomialValue({t}) = {Syntax.ToPolynomialSyntax().ToPolynomialBurst().Sample(t)}");
    }

    void PrintPolynomial()
    {
        Console.WriteLine($"Poly<{Syntax.ToPolynomialSyntax()}>");
    }

    void PrintTestValue4(Vector4 testT)
    {
        Console.WriteLine($"PolynomailValue({testT}) = {Syntax.ToPolynomialSyntax().ToPolynomialBurst().Sample(testT)}");
    }

    void PrintFreeSyntax()
    {
        Console.WriteLine($"FreeSyntax<{Syntax}>");
    }

    void PrintHlsl()
    {
        Console.WriteLine($"Hlsl<{Syntax.ToHlslCode()}>");
    }

    void PrintExpressionFromFreeSyntax()
    {
        Console.WriteLine($"ExprFromSyntax<{Syntax.ToExpression()}>");
    }

    public void Report(float t, Vector4 t4)
    {
        Console.WriteLine($"Test Case: {Name}");
        PrintExpression();
        PrintFreeSyntax();
        PrintExpressionFromFreeSyntax();
        PrintHlsl();
        PrintPolynomial();
        PrintTestValue(t);
        PrintTestValue4(t4);
        Console.WriteLine($"==========");
    }
}

class ExpressionTestCase : SyntaxTestCase
{
    readonly Expression<Func<float, float>> expr;

    public ExpressionTestCase(string name, Expression<Func<float, float>> expr)
        : base(name, expr.ToVaryingSyntax())
    {
        this.expr = expr;
    }

    protected override void PrintExpression()
    {
        Console.WriteLine($"Expr<{expr}>");
    }

    protected override void PrintTestValue(float t)
    {
        Console.WriteLine($"ReferenceValue({t}) = {expr.Compile()(t)}");
        base.PrintTestValue(t);
    }
}

static internal class Program
{
    static void Main(string[] args)
    {
        Expression<Func<float, float>> tPlusOneSquare = t => t * t + 2.0f * t + 1.0f;
        Expression<Func<float, float>> tPlusOne = t => t + 1.0f;
        Expression<Func<float, float>> tSquare = t => t * t;

        var expressionTestCases = new SyntaxTestCase[]
        {
            new ExpressionTestCase("constant 42", t => 42),
            new ExpressionTestCase("t", t => t),
            new ExpressionTestCase("3 * t", t => 3.0f * t),
            new ExpressionTestCase("complex expression", t => 1.0f + t + 2.0f * t),
            new ExpressionTestCase("t^2 + 2t + 1", tPlusOneSquare),
            new ExpressionTestCase("t + 1", tPlusOne),
        };

        var tPlusOneSquareHigerOrder = tSquare.ToVaryingSyntax().Substitute(tPlusOne.ToVaryingSyntax());
        var higherOrderTest = tPlusOneSquareHigerOrder.Offset(1.0f).Scale(2.0f);
        var syntaxTestCases = new SyntaxTestCase[]
        {
            new("composite (t+1)^2", tPlusOneSquareHigerOrder),
            // Polynomial optimizer provides partial evaluation to optimize chained add and mul 
            new("composite (t+1)^2 optimized", tPlusOneSquareHigerOrder.ToPolynomialSyntax().ToOptimized().ToVaryingSyntax()),
            new("composite (t+1)^2 offset 1 scale 2", higherOrderTest),
            new("composite (t+1)^2 offset 1 scale 2 optimized", higherOrderTest.ToPolynomialSyntax().ToOptimized().ToVaryingSyntax()),
            // Implemented lerp using meta-programming assuming lerp is not supported by VM
            new("lerp test", Varying.Lerp(tPlusOne.ToVaryingSyntax(), tSquare.ToVaryingSyntax(), Varying.Symbol)),
            new("random test case", Varying.Random),
            new("random between test case", Varying.Lerp(Varying.Lit(0.0f), Varying.Symbol, Varying.Random))
        };
        var testCases = expressionTestCases.Concat(syntaxTestCases).ToArray();

        foreach (var tc in testCases)
        {
            tc.Report((float)Math.PI, new((float)Math.PI, -1.0f, 0.0f, 1.0f));
        }
    }
}
