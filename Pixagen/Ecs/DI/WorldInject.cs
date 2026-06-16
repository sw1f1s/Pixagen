namespace Pixagen.Ecs.DI {
    public struct WorldInject : IDataInject {
        private IWorld _value;

        public readonly IWorld Value => _value;

        public WorldInject(IWorld world) {
            _value = world;
        }
        
        void IDataInject.Fill(ISystems systems) {
            _value = systems.World;
        }

        public readonly Entity Create<T>() where T : struct, IComponent {
            return Value.CreateEntity<T>();
        }

        public readonly bool IsAlive(Entity entity) {
            return entity != Entity.Empty && Value.EntityIsAlive(in entity);
        }

        public readonly Entity Copy(Entity entity) {
#if DEBUG
            if (!IsAlive(entity)) {
                throw new System.Exception($"{entity} is dead.");
            }
#endif
            return Value.CopyEntity(in entity);
        }

        public readonly void Destroy(Entity entity) {
#if DEBUG
            if (!IsAlive(entity)) {
                throw new System.Exception($"{entity} is dead.");
            }
#endif
            Value.DestroyEntity(in entity);
        }
    }   
}
