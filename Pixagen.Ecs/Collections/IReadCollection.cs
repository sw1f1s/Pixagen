namespace Pixagen.Ecs.Collections
{
    public interface IReadCollection
    {
        int GetCount();
        object GetItem(int index);
    }
}