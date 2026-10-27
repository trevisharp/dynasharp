namespace Dynasharp.Core.Exceptions;

public class InvalidAlgorithmReturnException(Type? type) : Exception
{
    public override string Message => 
        $"The value {type?.ToString() ?? "null"} is not a valid Algorithm object to return in a optimized function.";
}