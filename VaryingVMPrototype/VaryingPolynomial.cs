using System.Numerics;

namespace VaryingFromExpression;

static partial class Varying
{
    static readonly IVaryingSemantic<IPolynomialSyntax> k_PolynomialSyntaxSemantic =
        new FreeVaryingSemantic<IPolynomialSyntax>(
            (_, _) => Polynomial.Polynomial1(new(0.0f, 1.0f)),
            (_, _) => Polynomial.RandomUniformZeroOne(),
            (_, _, value) => Polynomial.Polynomial0(value),
            (_, _, left, right) => Polynomial.Add(left, right),
            (_, _, left, right) => Polynomial.Mul(left, right),
            (_, _, _, _, _) => throw new NotSupportedException()
        );

    static readonly IVaryingSemantic<IVaryingSyntax> k_LoweringLerpSemantic = new FreeVaryingSemantic<IVaryingSyntax>(
        (_, e) => e,
        (_, e) => e,
        (_, e, _) => e,
        (_, _, a, b) => Add(a, b),
        (_, _, x, y) => Multiply(x, y),
        (_, _, x, y, s) => Add(Multiply(FromExpression(t => 1 - t).Substitute(s), x), Multiply(s, y))
    );

    public static IPolynomialSyntax ToPolynomialSyntax(this IVaryingSyntax code)
    {
        // k_PolynomialSyntaxSemantic is not type safe, 
        // it is designed to not implement lerp
        // we need evaluate lerp via meta evaluation
        return code.Evaluate(k_LoweringLerpSemantic).Evaluate(k_PolynomialSyntaxSemantic);
    }

    static IVaryingSyntax FromPolynomial(float a0) => new LitFreeVaryingSyntax(a0);
    static IVaryingSyntax FromPolynomial(Vector2 a01) => Add(Multiply(Lit(a01.Y), Symbol), FromPolynomial(a01.X));
    static IVaryingSyntax FromPolynomial(Vector3 a012) => Add(Multiply(Lit(a012.Z), Multiply(Symbol, Symbol)), FromPolynomial(new Vector2(a012.X, a012.Y)));
    static IVaryingSyntax FromPolynomial(Vector4 a0123) => Add(Multiply(Lit(a0123.W), Multiply(Symbol, Multiply(Symbol, Symbol))), FromPolynomial(new Vector3(a0123.X, a0123.Y, a0123.Z)));
    static readonly IPolynomialSemantic<IVaryingSyntax> k_PolynomialSyntaxAsVaryingSyntaxPolynomialSemantic =
        new FreePolynomialSemantic<IVaryingSyntax>(
            static (_, a0) => FromPolynomial(a0),
            static (_, a01) => FromPolynomial(a01),
            static (_, a012) => FromPolynomial(a012),
            static (_, a0123) => FromPolynomial(a0123),
            static (_) => Random,
            static (_, x, y) => Add(x, y),
            static (_, x, y) => Multiply(x, y)
        );
    public static IVaryingSyntax ToVaryingSyntax(this IPolynomialSyntax code) => code.Evaluate(k_PolynomialSyntaxAsVaryingSyntaxPolynomialSemantic);
}
