using System.Dynamic;

namespace Dynasharp;

public class OptimizedFunction(Algorithm algorithm) : DynamicObject
{
    readonly Algorithm algorithm = algorithm;
}