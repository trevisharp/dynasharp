namespace Dynasharp.Exceptions;

public class InvalidAlgorithmTypeException(Type type) : Exception
{
    public override string Message => 
        $"The type {type} is not a valid IAlgorithmType with empty constructor.";
}