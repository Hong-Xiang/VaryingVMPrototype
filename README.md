# VaryingVMPrototype

A prototype implementation of considering Varyings as embedded DSL.

With considering eDSL implementation, we gain the following extra features:

## Scale/Offset by Substitution

When $`f`$ is polynomial, equations like following form could be partial evaluated

```math
g(t) = f(t * 3)
```

We can derive this by hand for its effect on coefficients,
but with eDSL implementation,
this could be automated by meta function `Subsititute`

## Composite of varyings

assuming there are two `Varying`s to be composite,
such as velocity is function of time,
and size if function of velocity,
with help of `Subsititue`,
this could be directly expressed.

## Compile to Multiple Accelerated Intrinsics

By eDSL, we can write code once and get multiple different implementations, such as:

### Multiple Interpreters

In this repository, the following interpreters is included:

* Expression Tree -> C# JIT
* C# Func -> C# AOT
* Polynomial -> Mimic some static accelerated platform
* Hlsl -> Mimic HLSL code generation (not finished yet)

### Multiple Semantics

Same code could generate two versions of Polynomials:

* over simple float t (IPolynomialOnT)
* over SIMD Vector4 t (IPolynomialOnT4)

### Dedicated Optimizations

Prototype implementation of

* Partial evaluation of polynomials' add/mul
* Optimize higher order polynomials to lower order if high order coefficient is zero
* Optimize add by zero expressions



