namespace Dynasharp;

using Exceptions;

public static class Dy
{
    /// <summary>
    /// Optimize a algorithm construct over algorithm parameters.
    /// </summary>
    public static OptimizedFunction Optimize<T, R>(Func<T, R> function)
        where T : Algorithm
        where R : Algorithm
    {
        ArgumentNullException.ThrowIfNull(function);

        var result = function.DynamicInvoke([ ..
            from param in function.Method.GetParameters()
            select CreateObject(param.ParameterType)
        ]);

        if (result is not Algorithm algorithm)
            throw new InvalidAlgorithmReturnException(result?.GetType());

        return new (algorithm);
    }
    
    static Algorithm CreateObject(Type type)
    {
        if (!type.IsAssignableTo(typeof(Algorithm)))
            throw new InvalidAlgorithmTypeException(type);

        var constructor = type.GetConstructor([]) 
            ?? throw new InvalidAlgorithmTypeException(type);
        
        return constructor.Invoke([]) as Algorithm
            ?? throw new InvalidAlgorithmTypeException(type);
    }
}