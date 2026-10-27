namespace Dynasharp;

using Core;

public static class Dy
{
    /// <summary>
    /// Optimize a algorithm construct over algorithm parameters.
    /// </summary>
    public static OptimizedFunction Optimize<R>(Func<R> function)
        => OptimizedFunction.Optimize(function);
    
    /// <summary>
    /// Optimize a algorithm construct over algorithm parameters.
    /// </summary>
    public static OptimizedFunction Optimize<T, R>(Func<T, R> function)
        => OptimizedFunction.Optimize(function);
    
    /// <summary>
    /// Optimize a algorithm construct over algorithm parameters.
    /// </summary>
    public static OptimizedFunction Optimize<T1, T2, R>(Func<T1, T2, R> function)
        => OptimizedFunction.Optimize(function);
}