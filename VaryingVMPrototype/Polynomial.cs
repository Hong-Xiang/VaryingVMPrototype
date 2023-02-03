using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using VaryingFromExpression;

interface IPolynomialSemantic<TR>
{
    TR Polynomial0(in IPolynomialSyntax e, in float a0);
    TR Polynomial1(in IPolynomialSyntax e, in Vector2 a01);
    TR Polynomial2(in IPolynomialSyntax e, in Vector3 a012);
    TR Polynomial3(in IPolynomialSyntax e, in Vector4 a0123);
    TR RandomUniformZeroOne(in IPolynomialSyntax e);
    TR Add(in IPolynomialSyntax e, in TR x, in TR y);
    TR Mul(in IPolynomialSyntax e, in TR x, in TR y);
}

interface IPolynomialSyntax
{
    TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern);
}

interface IPolynomialFreeSyntax : IPolynomialSyntax { }

readonly record struct FreePolynomial0Syntax(float A0) : IPolynomialFreeSyntax
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Polynomial0(this, A0);
}

readonly record struct FreePolynomial1Syntax(Vector2 A01) : IPolynomialFreeSyntax
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Polynomial1(this, A01);
}

readonly record struct FreePolynomial2Syntax(Vector3 A012) : IPolynomialFreeSyntax
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Polynomial2(this, A012);
}

readonly record struct FreePolynomial3Syntax(Vector4 A0123) : IPolynomialFreeSyntax
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Polynomial3(this, A0123);
}

readonly record struct FreeRandomUniformZeroOneSyntax() : IPolynomialFreeSyntax
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.RandomUniformZeroOne(this);
}

readonly record struct FreePolynomialAddSyntax(IPolynomialSyntax A, IPolynomialSyntax B) : IPolynomialFreeSyntax
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Add(this, A.Evaluate(pattern), B.Evaluate(pattern));
}

readonly record struct FreePolynomialMulSyntax(IPolynomialSyntax X, IPolynomialSyntax Y) : IPolynomialFreeSyntax
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Mul(this, X.Evaluate(pattern), Y.Evaluate(pattern));
}

readonly record struct FreePolynomialSemantic<TR>(
    Func<IPolynomialSyntax, float, TR> polynomial0,
    Func<IPolynomialSyntax, Vector2, TR> polynomial1,
    Func<IPolynomialSyntax, Vector3, TR> polynomial2,
    Func<IPolynomialSyntax, Vector4, TR> polynomial3,
    Func<IPolynomialSyntax, TR> random,
    Func<IPolynomialSyntax, TR, TR, TR> add,
    Func<IPolynomialSyntax, TR, TR, TR> mul
) : IPolynomialSemantic<TR>
{
    public TR Polynomial0(in IPolynomialSyntax e, in float a0)
        => polynomial0(e, a0);

    public TR Polynomial1(in IPolynomialSyntax e, in Vector2 a01)
        => polynomial1(e, a01);

    public TR Polynomial2(in IPolynomialSyntax e, in Vector3 a012)
        => polynomial2(e, a012);

    public TR Polynomial3(in IPolynomialSyntax e, in Vector4 a0123)
        => polynomial3(e, a0123);

    public TR RandomUniformZeroOne(in IPolynomialSyntax e)
        => random(e);

    public TR Add(in IPolynomialSyntax e, in TR x, in TR y)
        => add(e, x, y);

    public TR Mul(in IPolynomialSyntax e, in TR x, in TR y)
        => mul(e, x, y);
}

record struct PolynomialOptimizerSemantic : IPolynomialSemantic<IPolynomialSyntax>
{
    public IPolynomialSyntax Polynomial0(in IPolynomialSyntax e, in float a0) => e;

    public IPolynomialSyntax Polynomial1(in IPolynomialSyntax e, in Vector2 a01) =>
        a01.Y == 0.0f ? Polynomial.Polynomial0(a01.X) : e;

    public IPolynomialSyntax Polynomial2(in IPolynomialSyntax e, in Vector3 a012) => e;

    public IPolynomialSyntax Polynomial3(in IPolynomialSyntax e, in Vector4 a0123) => e;

    public IPolynomialSyntax RandomUniformZeroOne(in IPolynomialSyntax e) => e;

    // Add and Mul would be partial evaluated
    public IPolynomialSyntax Add(in IPolynomialSyntax e, in IPolynomialSyntax left, in IPolynomialSyntax right)
    {
        return (left, right) switch
        {
            (FreePolynomial0Syntax pl, FreePolynomial0Syntax pr) => pl.A0 == 0.0f ? pr : Polynomial.Polynomial0(pl.A0 + pr.A0),
            (FreePolynomial0Syntax pl, FreePolynomial1Syntax pr) => Polynomial.Polynomial1(new Vector2(pl.A0, 0.0f) + pr.A01),
            (FreePolynomial0Syntax pl, FreePolynomial2Syntax pr) => Polynomial.Polynomial2(new Vector3(pl.A0, 0.0f, 0.0f) + pr.A012),
            (FreePolynomial0Syntax pl, FreePolynomial3Syntax pr) => Polynomial.Polynomial3(new Vector4(pl.A0, 0.0f, 0.0f, 0.0f) + pr.A0123),
            (FreePolynomial1Syntax pl, FreePolynomial0Syntax pr) => Add(Polynomial.Add(pr, pl), pr, pl),
            (FreePolynomial1Syntax pl, FreePolynomial1Syntax pr) => Polynomial.Polynomial1(pl.A01 + pr.A01),
            (FreePolynomial1Syntax pl, FreePolynomial2Syntax pr) => Polynomial.Polynomial2(new Vector3(pl.A01, 0.0f) + pr.A012),
            (FreePolynomial1Syntax pl, FreePolynomial3Syntax pr) => Polynomial.Polynomial3(new Vector4(pl.A01, 0.0f, 0.0f) + pr.A0123),
            (FreePolynomial2Syntax pl, FreePolynomial0Syntax pr) => Add(Polynomial.Add(pr, pl), pr, pl),
            (FreePolynomial2Syntax pl, FreePolynomial1Syntax pr) => Add(Polynomial.Add(pr, pl), pr, pl),
            (FreePolynomial2Syntax pl, FreePolynomial2Syntax pr) => Polynomial.Polynomial2(pl.A012 + pr.A012),
            (FreePolynomial2Syntax pl, FreePolynomial3Syntax pr) => Polynomial.Polynomial3(new Vector4(pl.A012, 0.0f) + pr.A0123),
            (FreePolynomial3Syntax pl, FreePolynomial0Syntax pr) => Add(Polynomial.Add(pr, pl), pr, pl),
            (FreePolynomial3Syntax pl, FreePolynomial1Syntax pr) => Add(Polynomial.Add(pr, pl), pr, pl),
            (FreePolynomial3Syntax pl, FreePolynomial2Syntax pr) => Add(Polynomial.Add(pr, pl), pr, pl),
            (FreePolynomial3Syntax pl, FreePolynomial3Syntax pr) => Polynomial.Polynomial3(pl.A0123 + pr.A0123),
            _ => e
        };
    }

    static Vector3 MulT1(Vector2 v) => new(0.0f, v.X, v.Y);
    static Vector4 MulT1(Vector3 v) => new(0.0f, v.X, v.Y, v.Z);

    public IPolynomialSyntax Mul(in IPolynomialSyntax e, in IPolynomialSyntax x, in IPolynomialSyntax y)
    {
        return (x, y) switch
        {
            (FreePolynomial0Syntax pl, FreePolynomial0Syntax pr) => Polynomial.Polynomial0(pl.A0 * pr.A0),
            (FreePolynomial0Syntax pl, FreePolynomial1Syntax pr) => Polynomial.Polynomial1(pl.A0 * pr.A01),
            (FreePolynomial0Syntax pl, FreePolynomial2Syntax pr) => Polynomial.Polynomial2(pl.A0 * pr.A012),
            (FreePolynomial0Syntax pl, FreePolynomial3Syntax pr) => Polynomial.Polynomial3(pl.A0 * pr.A0123),
            (FreePolynomial1Syntax pl, FreePolynomial0Syntax pr) => Mul(Polynomial.Mul(pr, pl), pr, pl),
            (FreePolynomial1Syntax pl, FreePolynomial1Syntax pr) => Polynomial.Polynomial2(new Vector3(pl.A01.X * pr.A01, 0.0f) + pl.A01.Y * MulT1(pr.A01)),
            (FreePolynomial1Syntax pl, FreePolynomial2Syntax pr) => Polynomial.Polynomial3(new Vector4(pl.A01.X * pr.A012, 0.0f) + pl.A01.Y * MulT1(pr.A012)),
            (FreePolynomial2Syntax pl, FreePolynomial0Syntax pr) => Mul(Polynomial.Mul(pr, pl), pr, pl),
            (FreePolynomial2Syntax pl, FreePolynomial1Syntax pr) => Mul(Polynomial.Mul(pr, pl), pr, pl),
            (FreePolynomial3Syntax pl, FreePolynomial0Syntax pr) => Mul(Polynomial.Mul(pr, pl), pr, pl),
            _ => e
        };
    }
}

static class Polynomial
{
    public static FreePolynomial0Syntax Polynomial0(float a0) => new(a0);
    public static FreePolynomial1Syntax Polynomial1(Vector2 a01) => new(a01);
    public static FreePolynomial2Syntax Polynomial2(Vector3 a012) => new(a012);
    public static FreePolynomial3Syntax Polynomial3(Vector4 a0123) => new(a0123);
    public static FreeRandomUniformZeroOneSyntax RandomUniformZeroOne() => new();
    public static FreePolynomialAddSyntax Add(IPolynomialSyntax x, IPolynomialSyntax y) => new(x, y);
    public static FreePolynomialMulSyntax Mul(IPolynomialSyntax x, IPolynomialSyntax y) => new(x, y);

    static readonly IPolynomialSemantic<IPolynomialBurst> k_BurstPolynomialSemantic = new FreePolynomialSemantic<IPolynomialBurst>(
        (_, a0) => new Polynomial0(a0),
        (_, a01) => new Polynomial1(a01),
        (_, a012) => new Polynomial2(a012),
        (_, a0123) => new Polynomial3(a0123),
        (_) => new RandomUniformZeroOneVarying(new Random()),
        (_, x, y) => new AddPolynomial(x, y),
        (_, x, y) => new MulPolynomial(x, y)
    );

    public static IPolynomialBurst ToPolynomialBurst(this IPolynomialSyntax code) => code.Evaluate(k_BurstPolynomialSemantic);

    readonly static PolynomialOptimizerSemantic k_PolynomialOptimizerSemantic = new();

    public static IPolynomialSyntax ToOptimized(this IPolynomialSyntax code)
    {
        return code.Evaluate(k_PolynomialOptimizerSemantic);
    }
}

public interface IPolynomialOnT
{
    public float Sample(float t);
}

public interface IPolynomialOnT4
{
    public Vector4 Sample(Vector4 t);
}

interface IPolynomialBurst : IPolynomialSyntax, IPolynomialOnT, IPolynomialOnT4 { }

readonly record struct Polynomial0(float Value) : IPolynomialBurst
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Sample(float t) => Value;

    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Polynomial0(this, Value);
    public Vector4 Sample(Vector4 t) => new(Value, Value, Value, Value);
}

readonly record struct Polynomial1(Vector2 Coefficients) : IPolynomialBurst
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Sample(float t) => Coefficients.Y * t + Coefficients.X;

    public Vector4 Sample(Vector4 t) => Coefficients.Y * t + Vector4.One * Coefficients.X;

    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Polynomial1(this, Coefficients);
}

readonly record struct Polynomial2(Vector3 Coefficients) : IPolynomialBurst
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Sample(float t) => t * (t * Coefficients.Z + Coefficients.Y) + Coefficients.X;

    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Polynomial2(this, Coefficients);

    public Vector4 Sample(Vector4 t) =>
        t * (t * Coefficients.Z * Vector4.One + Coefficients.Y * Vector4.One) + Coefficients.X * Vector4.One;
}

readonly record struct Polynomial3(Vector4 Coefficients) : IPolynomialBurst
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Sample(float t) => t * (t * (t * Coefficients.W + Coefficients.Z) + Coefficients.Y) + Coefficients.X;

    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern) => pattern.Polynomial3(this, Coefficients);

    public Vector4 Sample(Vector4 t) =>
        t * (t * (t * Coefficients.W * Vector4.One + Coefficients.Z * Vector4.One) + Coefficients.Y * Vector4.One) + Coefficients.X * Vector4.One;
}

readonly record struct RandomUniformZeroOneVarying(Random Random) : IPolynomialBurst
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Sample(float t) => Random.NextSingle();

    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern)
        => pattern.RandomUniformZeroOne(this);

    public Vector4 Sample(Vector4 t) => new(Random.NextSingle(), Random.NextSingle(), Random.NextSingle(), Random.NextSingle());
}

readonly record struct AddPolynomial(IPolynomialBurst A, IPolynomialBurst B) : IPolynomialBurst
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern)
        => pattern.Add(this, A.Evaluate(pattern), B.Evaluate(pattern));

    public float Sample(float t) => A.Sample(t) + B.Sample(t);

    public Vector4 Sample(Vector4 t) => A.Sample(t) + B.Sample(t);
}

readonly record struct MulPolynomial(IPolynomialBurst A, IPolynomialBurst B) : IPolynomialBurst
{
    public TR Evaluate<TR>(in IPolynomialSemantic<TR> pattern)
        => pattern.Mul(this, A.Evaluate(pattern), B.Evaluate(pattern));

    public float Sample(float t) => A.Sample(t) * B.Sample(t);

    public Vector4 Sample(Vector4 t) => A.Sample(t) * B.Sample(t);
}
