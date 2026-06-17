namespace Pixagen.Ecs.DI
{
    public interface IDataInject
    {
        void Fill(ISystems systems);
    }

    public interface IInclude
    {
        FilterMask GetMask();
    }

    public interface IExclude
    {
        FilterMask GetMask();
    }

    public interface ICustomDataInject
    {
        void Fill(object[] injects);
    }

    public interface IAfterInject
    {
        void AfterInject();
    }

    public interface IDisposeInject
    {
        void DisposeInject();
    }
}
