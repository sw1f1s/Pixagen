using System;
using System.Collections.Generic;
using Pixagen.Ecs.Collections;

namespace Pixagen.Ecs.Runtime {
    public interface IWorld : IDisposable {
        byte Id { get; }
        Options Options { get; }
        internal bool IsAlive { get; }
        internal IComponentsStorage ComponentsStorage { get; }
        internal ref SparseArray<EntityData> Entities { get; }
#if DEBUG
        event Action<IWorld, Entity> OnCreateEntity;
        event Action<IWorld, Entity> OnCopyEntity;
        event Action<IWorld, Entity> OnDestroyEntity;
        event Action<IWorld, Entity, Type> OnAddComponent;
        event Action<IWorld, Entity, Type> OnRemoveComponent;
        IEnumerable<Entity> AllEntities();
#endif
        
        Entity CreateEntity<T>() where T : struct, IComponent;
        internal Entity CreateEntity(int id, int gen);
        internal bool EntityIsAlive(in Entity entity);
        internal void DestroyEntity(in Entity entity);
        Entity TryGetEntity(int id);
        Entity CopyEntity(in Entity entity);

        internal bool HasComponent<T>(in Entity entity) where T : struct, IComponent;
        internal ref T GetComponent<T>(in Entity entity) where T : struct, IComponent;
        
        internal void AddComponent<T>(in Entity entity, in T component) where T : struct, IComponent;
        internal void ReplaceComponent<T>(in Entity entity, in T component) where T : struct, IComponent;
        internal ref T SetComponent<T>(in Entity entity) where T : struct, IComponent;
        internal void RemoveComponent<T>(in Entity entity) where T : struct, IComponent;
        internal void RemoveComponent(in Entity entity, int componentIdx);

        internal ComponentStorage<T> GetComponentStorage<T>() where T : struct, IComponent;
        internal IComponentStorage GetComponentStorage(int componentId);
        internal bool HasComponentStorage(int componentId);
        
        Filter GetFilter(FilterMask mask);
        void Clear();
    }   
}
