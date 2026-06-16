using Pixagen.Ecs.Collections;

namespace Pixagen.Ecs.Runtime {
    public interface IPoolFactory {
        PooledList<T> Rent<T>(int initialCapacity = 4);
    }
}