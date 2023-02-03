using System.Numerics;

namespace VaryingFromExpression;

interface IVaryingSemantic<TR>
{
    TR Symbol(IVaryingSyntax e);
    TR Random(IVaryingSyntax e);
    TR Lit(IVaryingSyntax e, float value);
    TR Add(IVaryingSyntax e, TR left, TR right);
    TR Multiply(IVaryingSyntax e, TR left, TR right);
    TR Lerp(IVaryingSyntax e, TR x, TR y, TR s);
}

readonly record struct FreeVaryingSemantic<TR>(
    Func<FreeVaryingSemantic<TR>, IVaryingSyntax, TR> symbol,
    Func<FreeVaryingSemantic<TR>, IVaryingSyntax, TR> random,
    Func<FreeVaryingSemantic<TR>, IVaryingSyntax, float, TR> lit,
    Func<FreeVaryingSemantic<TR>, IVaryingSyntax, TR, TR, TR> add,
    Func<FreeVaryingSemantic<TR>, IVaryingSyntax, TR, TR, TR> multiply,
    Func<FreeVaryingSemantic<TR>, IVaryingSyntax, TR, TR, TR, TR> lerp
) : IVaryingSemantic<TR>
{
    public TR Symbol(IVaryingSyntax e) => symbol(this, e);
    public TR Random(IVaryingSyntax e) => random(this, e);
    public TR Lit(IVaryingSyntax e, float value) => lit(this, e, value);
    public TR Add(IVaryingSyntax e, TR left, TR right) => add(this, e, left, right);
    public TR Multiply(IVaryingSyntax e, TR left, TR right) => multiply(this, e, left, right);
    public TR Lerp(IVaryingSyntax e, TR x, TR y, TR s) => lerp(this, e, x, y, s);
}

interface IVaryingSyntax
{
    TR Evaluate<TR>(IVaryingSemantic<TR> varyingSemantic);
}

readonly record struct SymbolFreeVaryingSyntax : IVaryingSyntax
{
    public TR Evaluate<TR>(IVaryingSemantic<TR> varyingSemantic) => varyingSemantic.Symbol(this);
}

readonly record struct RandomFreeVaryingSyntax : IVaryingSyntax
{
    public TR Evaluate<TR>(IVaryingSemantic<TR> varyingSemantic) => varyingSemantic.Random(this);
}

readonly record struct LitFreeVaryingSyntax(float Value) : IVaryingSyntax
{
    public TR Evaluate<TR>(IVaryingSemantic<TR> varyingSemantic) => varyingSemantic.Lit(this, Value);
}

readonly record struct AddFreeVaryingSyntax(IVaryingSyntax Left, IVaryingSyntax Right) : IVaryingSyntax
{
    public TR Evaluate<TR>(IVaryingSemantic<TR> varyingSemantic) => varyingSemantic.Add(this, Left.Evaluate(varyingSemantic), Right.Evaluate(varyingSemantic));
}

readonly record struct MultipleFreeVaryingSyntax(IVaryingSyntax Left, IVaryingSyntax Right) : IVaryingSyntax
{
    public TR Evaluate<TR>(IVaryingSemantic<TR> varyingSemantic) => varyingSemantic.Multiply(this, Left.Evaluate(varyingSemantic), Right.Evaluate(varyingSemantic));
}

readonly record struct LerpFreeVaryingSyntax(IVaryingSyntax X, IVaryingSyntax Y, IVaryingSyntax S) : IVaryingSyntax
{
    public TR Evaluate<TR>(IVaryingSemantic<TR> varyingSemantic) => varyingSemantic.Lerp(this, X.Evaluate(varyingSemantic), Y.Evaluate(varyingSemantic), S.Evaluate(varyingSemantic));
}

static partial class Varying
{
    public static SymbolFreeVaryingSyntax Symbol = new();
    public static RandomFreeVaryingSyntax Random = new();
    public static LitFreeVaryingSyntax Lit(float value) => new(value);
    public static AddFreeVaryingSyntax Add(IVaryingSyntax l, IVaryingSyntax r) => new(l, r);
    public static MultipleFreeVaryingSyntax Multiply(IVaryingSyntax l, IVaryingSyntax r) => new(l, r);

    public static LerpFreeVaryingSyntax Lerp(IVaryingSyntax x, IVaryingSyntax y, IVaryingSyntax s) => new(x, y, s);

    static readonly IVaryingSemantic<IVaryingSyntax> k_FreeSyntaxVaryingSemantic = new FreeVaryingSemantic<IVaryingSyntax>(
        static (_, _) => new SymbolFreeVaryingSyntax(),
        static (_, _) => new RandomFreeVaryingSyntax(),
        static (_, _, value) => new LitFreeVaryingSyntax(value),
        static (_, _, a, b) => new AddFreeVaryingSyntax(a, b),
        static (_, _, a, b) => new MultipleFreeVaryingSyntax(a, b),
        static (_, _, x, y, s) => new LerpFreeVaryingSyntax(x, y, s)
    );

    public static IVaryingSyntax ToFreeSyntax(this IVaryingSyntax code) => code.Evaluate(k_FreeSyntaxVaryingSemantic);

    static readonly IVaryingSemantic<Func<float, float>> k_InterpreterVaryingSemantic = new FreeVaryingSemantic<Func<float, float>>(
        static (_, _) => t => t,
        static (_, _) => t => new Random().NextSingle(),
        static (_, _, value) => _ => value,
        static (_, _, fa, fb) => t => fa(t) + fb(t),
        static (_, _, fa, fb) => t => fa(t) * fb(t),
        static (_, _, fx, fy, fs) => t =>
        {
            var s = fs(t);
            return (1.0f - s) * fx(t) + s * fy(t);
        }
    );

    public static Func<float, float> ToFunc(this IVaryingSyntax code) => code.Evaluate(k_InterpreterVaryingSemantic);

    static readonly IVaryingSemantic<string> k_HlslVaryingSemantic = new FreeVaryingSemantic<string>(
        static (_, _) => "t",
        static (_, _) => "<random-not-support>",
        static (_, _, v) => v.ToString(),
        static (_, _, a, b) => $"({a} + {b})",
        static (_, _, a, b) => $"({a} * {b})",
        static (_, _, x, y, s) => $"lerp({x}, {y}, {s})"
    );

    public static string ToHlslCode(this IVaryingSyntax code) => code.Evaluate(k_HlslVaryingSemantic);

    // Higher-order Varyings 🎉
    static IVaryingSemantic<IVaryingSyntax> SubstituteSemantic(IVaryingSyntax NewSymbol) => new FreeVaryingSemantic<IVaryingSyntax>(
        (_, _) => NewSymbol,
        static (_, e) => e,
        static (_, e, _) => e,
        static (_, _, l, r) => Add(l, r),
        static (_, _, l, r) => Multiply(l, r),
        static (_, _, x, y, s) => Lerp(x, y, s)
    );

    public static IVaryingSyntax Substitute(this IVaryingSyntax code, IVaryingSyntax symbol) => code.Evaluate(SubstituteSemantic(symbol));

    public static IVaryingSyntax Scale(this IVaryingSyntax code, float scale) => code.Substitute(Multiply(Lit(scale), Symbol));
    public static IVaryingSyntax Offset(this IVaryingSyntax code, float scale) => code.Substitute(Add(Lit(scale), Symbol));
}
