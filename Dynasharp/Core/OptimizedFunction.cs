using System.Dynamic;

namespace Dynasharp.Core;

using Exceptions;

public class OptimizedFunction(Algorithm algorithm) : DynamicObject
{
    readonly Algorithm algorithm = algorithm;

    public static OptimizedFunction Optimize(Delegate function)
    {
        ArgumentNullException.ThrowIfNull(function);

        var parameters = GenerateAlgebraicParameters(function);
        var funcReturn = function.DynamicInvoke(parameters);

        if (funcReturn is not Algorithm algorithm)
            throw new InvalidAlgorithmReturnException(funcReturn?.GetType());

        return new (algorithm);
    }

    static object[] GenerateAlgebraicParameters(Delegate function)
    {
        var parameters = function.Method.GetParameters();
        return [ ..
            from param in parameters
            select MapType(param.ParameterType)
        ];
    }
    
    static Algorithm MapType(Type type)
    {
        if (!type.IsAssignableTo(typeof(IAlgorithmType)))
            throw new InvalidAlgorithmTypeException(type);

        var constructor = type.GetConstructor([]) 
            ?? throw new InvalidAlgorithmTypeException(type);
        
        var obj = constructor.Invoke([]) as IAlgorithmType
            ?? throw new InvalidAlgorithmTypeException(type);
        
        return obj.ToAlgorithm();
    }
}