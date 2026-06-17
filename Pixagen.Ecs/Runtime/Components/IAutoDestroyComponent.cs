namespace Pixagen.Ecs.Runtime
{
    public interface IAutoDestroyComponent<T> where T : struct, IComponent
    {
        public void Destroy(ref T c);
    }
}