using System;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Ecs {
    [TestFixture]
    public class WorldThreadTest {
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_FilterCreateEntityTreadJob(int count) {
            var world = WorldBuilder.Build();
            var filter1 = world.GetFilter(new FilterMask<Component1>());
            var filter2 = world.GetFilter(new FilterMask<Component2>());
            var createEntityFilterThread = new CreateEntityFilterThreadJob(world);
            var entities = new Entity[count];
            for (int i = 0; i < count; i++) {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1(0));
                entities[i] = entity;
            }
            
            createEntityFilterThread.Execute(filter1);
            Assert.That(filter2.GetCount(), Is.EqualTo(count));
            world.Dispose();
        }

        
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_FilterIncreaseComponentTreadJob(int count) {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var increaseComponent1FilterTread = new IncreaseComponent1FilterThreadJob(world);
            var entities = new Entity[count];
            for (int i = 0; i < count; i++) {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1(0));
                entities[i] = entity;
            }
           
            increaseComponent1FilterTread.Execute(filter);
            for (int i = 0; i < count; i++) {
                Assert.That(Access(entities[i]).Get<Component1>().Value, Is.EqualTo(1));
            }
            
            increaseComponent1FilterTread.Execute(filter);
            for (int i = 0; i < count; i++) {
                Assert.That(Access(entities[i]).Get<Component1>().Value, Is.EqualTo(2));
            }
            
            world.Dispose();
        }
        
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_FilterAddComponentTreadJob(int count) {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var addComponentFilterTread = new AddComponentFilterThreadJob(world);
            var entities = new Entity[count];
            for (int i = 0; i < count; i++) {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1(0));
                entities[i] = entity;
            }
           
            addComponentFilterTread.Execute(filter);
            for (int i = 0; i < count; i++) {
                Assert.That(Access(entities[i]).Has<Component2>(), Is.True, $"Component2 should exist on entity{entities[i]}");
                Assert.That(Access(entities[i]).Has<Component3>(), Is.True, $"Component3 should exist on entity{entities[i]}");
            }
            
            world.Dispose();
        }
        
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_FilterRemoveComponentTreadJob(int count) {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var removeComponentFilterTread = new RemoveComponentFilterThreadJob(world);
            var entities = new Entity[count];
            for (int i = 0; i < count; i++) {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1(0));
                Access(entity).Add(new Component2());
                entities[i] = entity;
            }
           
            removeComponentFilterTread.Execute(filter);
            for (int i = 0; i < count; i++) {
                Assert.That(Access(entities[i]).Has<Component1>(), Is.True, $"Component1 should exist on entity{entities[i]}");
                Assert.That(Access(entities[i]).Has<Component2>(), Is.False, $"Component2 should exist on entity{entities[i]}");
            }
            
            world.Dispose();
        }
        
        [OneTimeTearDown]
        public void Cleanup() {
            WorldBuilder.AllDestroy();
        }
        
        private class IncreaseComponent1FilterThreadJob : FilterThreadJob {
            private readonly ComponentInject<Component1> _component1;

            public IncreaseComponent1FilterThreadJob(IWorld world) {
                _component1 = new ComponentInject<Component1>(world);
            }

            protected override void ExecuteInternal(Entity entity) {
                ref var c = ref _component1.Get(entity);
                c.Value++;
            }
        }
        
        private class CreateEntityFilterThreadJob : FilterThreadJob {
            private readonly IWorld _world;
            public CreateEntityFilterThreadJob(IWorld world) {
                _world = world;
            }
            
            protected override void ExecuteInternal(Entity entity) {
                _world.CreateEntity<Component2>();
            }
        }
        
        private class AddComponentFilterThreadJob : FilterThreadJob {
            private readonly ComponentInject<Component2> _component2;
            private readonly ComponentInject<Component3> _component3;

            public AddComponentFilterThreadJob(IWorld world) {
                _component2 = new ComponentInject<Component2>(world);
                _component3 = new ComponentInject<Component3>(world);
            }

            protected override void ExecuteInternal(Entity entity) {
                _component2.Add(entity, new Component2());
                _component3.GetOrSet(entity);
            }
        }
        
        private class RemoveComponentFilterThreadJob : FilterThreadJob {
            private readonly ComponentInject<Component2> _component2;

            public RemoveComponentFilterThreadJob(IWorld world) {
                _component2 = new ComponentInject<Component2>(world);
            }

            protected override void ExecuteInternal(Entity entity) {
                _component2.Remove(entity);
            }
        }
    }   
}
