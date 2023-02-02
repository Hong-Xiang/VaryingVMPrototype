using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;

public interface IVarying<T>
    where T : struct
{
    interface IPattern<out TR>
    {
        TR Polynomial0(in Polynomial0 p);
        TR Polynomial1(in Polynomial1 p);
        TR Polynomial2(in Polynomial2 p);
        TR Polynomial3(in Polynomial3 p);
    }

    float Evaluate(float t);

    TR Match<TR>(in IPattern<TR> pattern);
}

public readonly record struct Polynomial0(float Value) : IVarying<float>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float t) => Value;


    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern) => pattern.Polynomial0(this);
}

public readonly record struct Polynomial1(Vector2 Coefficients) : IVarying<float>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float t) => Coefficients.Y * t + Coefficients.X;


    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern) => pattern.Polynomial1(this);
}

public readonly record struct Polynomial2(Vector3 Coefficients) : IVarying<float>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float t) => t * (t * Coefficients.Z + Coefficients.Y) + Coefficients.X;

    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern) => pattern.Polynomial2(this);
}

public readonly record struct Polynomial3(Vector4 Coefficients) : IVarying<float>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float t) => t * (t * (t * Coefficients.W + Coefficients.Z) + Coefficients.Y) + Coefficients.X;


    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern)
    {
        throw new NotImplementedException();
    }
}


public readonly struct LerpVarying<TVaryingA, TVaryingB, TVaryingWeight> : IVarying<float>
    where TVaryingA : IVarying<float>
    where TVaryingB : IVarying<float>
    where TVaryingWeight : IVarying<float>
{
    public readonly TVaryingA VA;
    public readonly TVaryingB VB;
    public readonly TVaryingWeight VW;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LerpVarying(TVaryingA va, TVaryingB vb, TVaryingWeight vw)
    {
        VA = va;
        VB = vb;
        VW = vw;
    }

    public static float Lerp(float a, float b, float s)
    {
        return a * s + b * (1 - s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float t)
    {
        return Lerp(VA.Evaluate(t), VB.Evaluate(t), VW.Evaluate((t)));
    }

    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern)
    {
        throw new NotImplementedException();
    }
}


public readonly struct MulVarying<TVA, TVB> : IVarying<float>
    where TVA : IVarying<float>
    where TVB : IVarying<float>
{
    public readonly TVA VA;
    public readonly TVB VB;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MulVarying(TVA va, TVB vb)
    {
        VA = va;
        VB = vb;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float t)
    {
        return VA.Evaluate(t) * VB.Evaluate(t);
    }

    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern)
    {
        throw new NotImplementedException();
    }
}

public readonly struct PeriodicalVarying<TV> : IVarying<float>
    where TV : IVarying<float>

{
    public readonly TV Varying;
    public readonly float Period;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PeriodicalVarying(TV v, float period)
    {
        Varying = v;
        Period = period;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float t)
    {
        return Varying.Evaluate(t % Period);
    }

    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern)
    {
        throw new NotImplementedException();
    }
}

public readonly struct TimeScaleOffsetVarying<TV> : IVarying<float>
    where TV : IVarying<float>

{
    public readonly float Scale;
    public readonly float Offset;
    public readonly TV Varying;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TimeScaleOffsetVarying(TV v, float scale, float offset)
    {
        Varying = v;
        Scale = scale;
        Offset = offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Evaluate(float t)
    {
        return Varying.Evaluate(t * Scale + Offset);
    }

    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern)
    {
        throw new NotImplementedException();
    }
}

public readonly struct ConcatVarying<TVA, TVB, T> : IVarying<T>
    where T : struct
    where TVA : struct, IVarying<T>
    where TVB : struct, IVarying<T>
{
    public readonly TVA VA;
    public readonly TVB VB;
    public readonly float SwitchTime;

    public ConcatVarying(TVA va, TVB vb, float switchTime)
    {
        VA = va;
        VB = vb;
        SwitchTime = switchTime;
    }

    public T Evaluate(float t)
    {
        // return t <= SwitchTime ? VA.Evaluate(t) : VB.Evaluate(t - SwitchTime);
        throw new NotImplementedException();
    }

    public TR Match<TR>(in IVarying<T>.IPattern<TR> pattern)
    {
        throw new NotImplementedException();
    }

    float IVarying<T>.Evaluate(float t)
    {
        throw new NotImplementedException();
    }
}

public readonly struct SelectVarying<TVA, TVB> : IVarying<float>
    where TVA : struct, IVarying<float>
    where TVB : struct, IVarying<float>
{
    public readonly TVA VA;
    public readonly TVB VB;
    public readonly float SwitchTime;

    public SelectVarying(TVA va, TVB vb, float switchTime)
    {
        VA = va;
        VB = vb;
        SwitchTime = switchTime;
    }

    public float Evaluate(float t)
    {
        // return math.select(VB.Evaluate(t), VA.Evaluate(t), t <= SwitchTime);
        throw new NotImplementedException();
    }

    public TR Match<TR>(in IVarying<float>.IPattern<TR> pattern)
    {
        throw new NotImplementedException();
    }
}

public static class Varying
{
    public static Polynomial1 T() => Polynomial(0.0f, 1.0f);

    public static Polynomial0 Polynomial(float a0) => new(a0);

    public static Polynomial1 Polynomial(float a0, float a1) => new(new(a0, a1));

    public static Polynomial2 Polynomial(float a0, float a1, float a2) => new(new(a0, a1, a2));

    public static Polynomial3 Polynomial(float a0, float a1, float a2, float a3) => new(new(a0, a1, a2, a3));


    public static Polynomial0 Add(float a, Polynomial0 b) => new(a + b.Value);
    public static Polynomial0 Add(Polynomial0 a, Polynomial0 b) => new(a.Value + b.Value);


    public static Polynomial1 Add(float a, Polynomial1 b) => new(b.Coefficients with { X = b.Coefficients.X + a });

    public static Polynomial1 Add(Polynomial0 a, Polynomial1 b) =>
        new(b.Coefficients with { X = b.Coefficients.X + a.Value });

    public static Polynomial1 Add(Polynomial1 a, Polynomial1 b) => new(a.Coefficients + b.Coefficients);


    public static Polynomial2 Add(float a, Polynomial2 b) => new(b.Coefficients with { X = b.Coefficients.X + a });

    public static Polynomial2 Add(Polynomial0 a, Polynomial2 b) =>
        new(b.Coefficients with { X = b.Coefficients.X + a.Value });

    public static Polynomial2 Add(Polynomial1 a, Polynomial2 b) =>
        new(b.Coefficients + new Vector3(a.Coefficients.X, a.Coefficients.Y, 0.0f));

    public static Polynomial2 Add(Polynomial2 a, Polynomial2 b) => new(a.Coefficients + b.Coefficients);


    public static Polynomial3 Add(float a, Polynomial3 b) => new(b.Coefficients with { X = b.Coefficients.X + a });

    public static Polynomial3 Add(Polynomial0 a, Polynomial3 b) =>
        new(b.Coefficients with { X = b.Coefficients.X + a.Value });

    public static Polynomial3 Add(Polynomial1 a, Polynomial3 b) =>
        new(b.Coefficients with { X = b.Coefficients.X + a.Coefficients.X });

    public static Polynomial3 Add(Polynomial2 a, Polynomial3 b) =>
        new(b.Coefficients + new Vector4(a.Coefficients.X, a.Coefficients.Y, a.Coefficients.Z, 0.0f));

    public static Polynomial3 Add(Polynomial3 a, Polynomial3 b) => new(a.Coefficients + b.Coefficients);


    public static Polynomial0 Mul(float a, Polynomial0 b) => new(a * b.Value);

    public static Polynomial0 Mul(Polynomial0 a, Polynomial0 b) => new(a.Value * b.Value);

    public static Polynomial1 Mul(float a, Polynomial1 b) => new(a * b.Coefficients);

    public static Polynomial1 Mul(Polynomial0 a, Polynomial1 b) => new(a.Value * b.Coefficients);


    public static Polynomial2 Mul(Polynomial1 a, Polynomial1 b)
        => new
        (
            new Vector3(a.Coefficients * b.Coefficients.X, 0.0f) +
            new Vector3(0.0f, a.Coefficients.X, a.Coefficients.Y) * b.Coefficients.Y
        );

    public static Polynomial2 Mul(Polynomial0 a, Polynomial2 b)
        => new(b.Coefficients * a.Value);

    public static Polynomial3 Mul(float a, Polynomial3 b)
        => new(b.Coefficients * a);


    public static Polynomial3 Mul(Polynomial0 a, Polynomial3 b)
        => new(b.Coefficients * a.Value);

    public static Polynomial3 Mul(Polynomial1 a, Polynomial2 b)
        => new
        (
            new Vector4(a.Coefficients * b.Coefficients.X, 0.0f, 0.0f) +
            new Vector4(0.0f, a.Coefficients.X, a.Coefficients.Y, 0.0f) * b.Coefficients.Y +
            new Vector4(0.0f, 0.0f, a.Coefficients.X, a.Coefficients.Y) * b.Coefficients.Z
        );


    public static PeriodicalVarying<TV> periodical<TV>(TV v, float period) where TV : IVarying<float> => new(v, period);

    public static TimeScaleOffsetVarying<TV> scaleOffset<TV>(TV v, float scale, float offset)
        where TV : IVarying<float> => new(v, scale, offset);

    public static ConcatVarying<TVA, TVB, T> concat<TVA, TVB, T>(TVA va, TVB vb, float switchTime)
        where T : struct
        where TVA : struct, IVarying<T>
        where TVB : struct, IVarying<T>
        => new(va, vb, switchTime);

    public static SelectVarying<TVA, TVB> select<TVA, TVB>(TVA va, TVB vb, float switchTime)
        where TVA : struct, IVarying<float>
        where TVB : struct, IVarying<float>
        => new(va, vb, switchTime);

    public static float integral<TV>(TV v, float t0, float t1) where TV : IVarying<float>
    {
        var t = (t0 + t1) * 0.5f;
        var dt = (t1 - t0);
        return v.Evaluate(t) * dt;
    }

    public static LerpVarying<TVaryingA, TVaryingB, TVaryingWeight>
        lerp<TVaryingA, TVaryingB, TVaryingWeight>(TVaryingA va, TVaryingB vb, TVaryingWeight vw)
        where TVaryingA : IVarying<float>
        where TVaryingB : IVarying<float>
        where TVaryingWeight : IVarying<float>
        =>
            new(va, vb, vw);
}