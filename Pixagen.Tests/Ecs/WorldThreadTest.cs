using System;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.DI;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Ecs
{
    [TestFixture]
    public class WorldThreadTest
    {
        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_FilterCreateEntityDuringIteration(int count)
        {
            var world = WorldBuilder.Build();
            var filter1 = world.GetFilter(new FilterMask<Component1>());
            var filter2 = world.GetFilter(new FilterMask<Component2>());
            var entities = new Entity[count];
            for (int i = 0; i < count; i++)
            {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1(0));
                entities[i] = entity;
            }

            foreach (Entity _ in filter1)
            {
                world.CreateEntity<Component2>();
            }

            Assert.That(filter2.GetCount(), Is.EqualTo(count));
            world.Dispose();
        }


        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_FilterIncreaseComponentDuringIteration(int count)
        {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var component1 = new ComponentInject<Component1>(world);
            var entities = new Entity[count];
            for (int i = 0; i < count; i++)
            {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1(0));
                entities[i] = entity;
            }

            IncreaseComponent1(filter, component1);
            for (int i = 0; i < count; i++)
            {
                Assert.That(Access(entities[i]).Get<Component1>().Value, Is.EqualTo(1));
            }

            IncreaseComponent1(filter, component1);
            for (int i = 0; i < count; i++)
            {
                Assert.That(Access(entities[i]).Get<Component1>().Value, Is.EqualTo(2));
            }

            world.Dispose();
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_FilterAddComponentDuringIteration(int count)
        {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var component2 = new ComponentInject<Component2>(world);
            var component3 = new ComponentInject<Component3>(world);
            var entities = new Entity[count];
            for (int i = 0; i < count; i++)
            {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1(0));
                entities[i] = entity;
            }

            foreach (Entity entity in filter)
            {
                component2.Add(entity, new Component2());
                component3.GetOrSet(entity);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.That(Access(entities[i]).Has<Component2>(), Is.True, $"Component2 should exist on entity{entities[i]}");
                Assert.That(Access(entities[i]).Has<Component3>(), Is.True, $"Component3 should exist on entity{entities[i]}");
            }

            world.Dispose();
        }

        [TestCase(1)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_FilterRemoveComponentDuringIteration(int count)
        {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var component2 = new ComponentInject<Component2>(world);
            var entities = new Entity[count];
            for (int i = 0; i < count; i++)
            {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1(0));
                Access(entity).Add(new Component2());
                entities[i] = entity;
            }

            foreach (Entity entity in filter)
            {
                component2.Remove(entity);
            }

            for (int i = 0; i < count; i++)
            {
                Assert.That(Access(entities[i]).Has<Component1>(), Is.True, $"Component1 should exist on entity{entities[i]}");
                Assert.That(Access(entities[i]).Has<Component2>(), Is.False, $"Component2 should exist on entity{entities[i]}");
            }

            world.Dispose();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            WorldBuilder.AllDestroy();
        }

        private static void IncreaseComponent1(Filter filter, ComponentInject<Component1> component1)
        {
            foreach (Entity entity in filter)
            {
                ref var component = ref component1.Get(entity);
                component.Value++;
            }
        }
    }
}
