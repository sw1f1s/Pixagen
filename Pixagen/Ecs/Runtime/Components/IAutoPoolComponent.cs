namespace Pixagen.Ecs.Runtime
{
    public interface IAutoPoolComponent<T> where T : struct, IComponent
    {
        public void Reset(ref T c, IPoolFactory poolFactory);
        public void Destroy(ref T c, IPoolFactory poolFactory);
    }
}