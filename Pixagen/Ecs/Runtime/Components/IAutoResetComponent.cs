namespace Pixagen.Ecs.Runtime
{
    public interface IAutoResetComponent<T> where T : struct, IComponent
    {
        public void Reset(ref T c);
    }
}