namespace Pixagen.Ecs.Runtime
{
    internal class RemoveOneTickComponentSystem : ILateUpdateSystem
    {
        private readonly IWorld _world;
        public RemoveOneTickComponentSystem(IWorld world)
        {
            _world = world;
        }

        void ILateUpdateSystem.LateUpdate()
        {
            foreach (var componentId in _world.ComponentsStorage.OneTickStorages)
            {
                var storage = _world.ComponentsStorage.Get(componentId);
                var entities = storage.Entities;
                for (int i = storage.Count - 1; i >= 0; i--)
                {
                    int entityIdx = entities.DenseValues[i];
                    var entity = _world.Entities.Get(entityIdx).GetEntity();
                    _world.RemoveComponent(entity, componentId);
                }
            }
        }
    }
}
