using System.Linq.Expressions;

namespace Dynasharp.Core;

public class PipelineElement
{
    public PipelineElement Select(Func<dynamic, object> func)
    {
        return default;
    }
}