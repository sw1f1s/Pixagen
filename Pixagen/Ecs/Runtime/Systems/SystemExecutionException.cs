namespace Pixagen.Ecs.Runtime;

public readonly struct SystemExecutionException
{
    public SystemExecutionException(Exception exception, ISystem system, string stage)
    {
        Exception = exception;
        System = system;
        Stage = stage;
    }

    public Exception Exception { get; }
    public ISystem System { get; }
    public string Stage { get; }
}
