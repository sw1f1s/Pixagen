using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using Pixagen.Ecs.Runtime;
using Pixagen.Ecs.Collections;
using Pixagen.Ecs.DI;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Ecs {
    [TestFixture]
    public class WorldTest {
        [Test]
        public void Run_CreateWorlds() {
            var world1 = WorldBuilder.Build();
            var world2 = WorldBuilder.Build();
            var world3 = WorldBuilder.Build();
            var world4 = WorldBuilder.Build();
            
            Assert.That(WorldAlive(world1), Is.True);
            Assert.That(WorldAlive(world2), Is.True);
            Assert.That(WorldAlive(world3), Is.True);
            Assert.That(WorldAlive(world4), Is.True);
            
            world1.Dispose();
            world3.Dispose();
            Assert.That(WorldAlive(world1), Is.False);
            Assert.That(WorldAlive(world3), Is.False);
            
            var world5 = WorldBuilder.Build();
            var world6 = WorldBuilder.Build();
            
            Assert.That(WorldAlive(world1), Is.False);
            Assert.That(WorldAlive(world3), Is.False);
            Assert.That(WorldAlive(world5), Is.True);
            Assert.That(WorldAlive(world6), Is.True);
            
            WorldBuilder.AllDestroy();
        }
        
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void Run_CreateEntity(int count) {
            var world = WorldBuilder.Build();
            var filter1 = world.GetFilter(new FilterMask<Component1>());
            var filter2 = world.GetFilter(new FilterMask<Component2>());
            var filter3 = world.GetFilter(new FilterMask<Component3>());
            for (int i = 0; i < count; i++) {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1());
                Access(entity).Add(new Component2());
                Access(entity).GetOrSet<Component3>();
            }
            
            Assert.That(filter1.GetCount(), Is.EqualTo(count));
            Assert.That(filter3.GetCount(), Is.EqualTo(count));
            foreach (var filter in filter1) {
                Access(filter).Remove<Component1>();
            }
            
            Assert.That(filter1.GetCount(), Is.EqualTo(0));
            Assert.That(filter3.GetCount(), Is.EqualTo(count));
            
            foreach (var e in filter2) {
                var entity = world.CreateEntity<IsTestEntity>();
                Access(entity).Add(new Component1());
                Access(entity).Add(new Component2());
                Access(entity).GetOrSet<Component3>();
            }
            
            Assert.That(filter1.GetCount(), Is.EqualTo(count));
            Assert.That(filter2.GetCount(), Is.EqualTo(count * 2));
            Assert.That(filter3.GetCount(), Is.EqualTo(count * 2));
            
            world.Dispose();
        }
        [Test]
        public void Run_LifeEntity() {
            var world = WorldBuilder.Build();
            var filter1 = world.GetFilter(new FilterMask<Component1>());
            var filter2 = world.GetFilter(new FilterMask<Component2>());
            var filter3 = world.GetFilter(new FilterMask<Component3>());
            
            var entity1 = world.CreateEntity<IsTestEntity>();
            Access(entity1).GetOrSet<Component1>();
            Assert.That(filter1.GetCount(), Is.EqualTo(1));
            Assert.That(entity1.Id, Is.EqualTo(0));
            Assert.That(entity1.Gen, Is.EqualTo(1));
            
            var entity2 = world.CreateEntity<IsTestEntity>();
            Access(entity1).GetOrSet<Component2>();
            Assert.That(filter2.GetCount(), Is.EqualTo(1));
            Assert.That(entity2.Id, Is.EqualTo(1));
            Assert.That(entity2.Gen, Is.EqualTo(1));
            
            Access(entity1).Destroy();
            
            Assert.That(filter1.GetCount(), Is.EqualTo(0));
            Assert.That(Access(entity1).IsAlive(), Is.False);
            
            var entity3 = world.CreateEntity<IsTestEntity>();
            Access(entity3).GetOrSet<Component3>();
            Assert.That(filter1.GetCount(), Is.EqualTo(0));
            Assert.That(filter3.GetCount(), Is.EqualTo(1));
            Assert.That(entity3.Id, Is.EqualTo(0));
            Assert.That(entity3.Gen, Is.EqualTo(2));
            
            Access(entity2).Remove<IsTestEntity>();
            Assert.That(Access(entity2).IsAlive(), Is.False);
            
            world.Dispose();
        }

        [Test]
        public void Run_EntityPackedIntoLong() {
            Assert.That(Unsafe.SizeOf<Entity>(), Is.EqualTo(sizeof(long)));

            var world = WorldBuilder.Build();
            var entity = world.CreateEntity<IsTestEntity>();

            Assert.That(Entity.Empty.Id, Is.EqualTo(-1));
            Assert.That(Entity.Empty.Gen, Is.EqualTo(-1));
            Assert.That(Entity.Empty.WorldId, Is.EqualTo(0));
            Assert.That(entity.Id, Is.EqualTo(0));
            Assert.That(entity.Gen, Is.EqualTo(1));
            Assert.That(entity.WorldId, Is.EqualTo(world.Id));

            world.Dispose();
        }
        
        [Test]
        public void Run_CopyEntity() {
            var world = WorldBuilder.Build();
            var entity1 = world.CreateEntity<IsTestEntity>();
            Access(entity1).Add(new Component1(100));
            Access(entity1).GetOrSet<Component2>();
            Access(entity1).Add(new Component3(1));

            var copy = Access(entity1).Copy();
            Assert.That(Access(copy).Has<Component1>(), Is.True);
            Assert.That(Access(copy).Has<Component2>(), Is.True);
            Assert.That(Access(copy).Has<Component3>(), Is.True);
            
            Assert.That(Access(copy).Get<Component1>().Value,  Is.EqualTo(100));
            Assert.That(Access(copy).Get<Component2>().Value, Is.True);
            Assert.That(Access(copy).Get<Component3>().Value, Is.EqualTo(1f));

            var component1 = new ComponentInject<Component1>(world);
            component1.Get(entity1).Value = 50;
            Assert.That(Access(entity1).Get<Component1>().Value,  Is.EqualTo(50));
            Assert.That(Access(copy).Get<Component1>().Value,  Is.EqualTo(100));
            
            world.Dispose();
        }
        
        [Test]
        public void Run_Components() {
            var world = WorldBuilder.Build();
            
            var entity1 = world.CreateEntity<IsTestEntity>();
            Access(entity1).Add(new Component1(100));
            Assert.That(Access(entity1).Has<Component1>(), Is.True);
            
            var component1 = new ComponentInject<Component1>(world);
            ref var component = ref component1.Get(entity1);
            Assert.That(component.Value == 100, Is.True);
            
            component.Value = 200;
            Assert.That(Access(entity1).Get<Component1>().Value, Is.EqualTo(200));
            
            Access(entity1).Replace(new Component1(300));
            Assert.That(Access(entity1).Get<Component1>().Value, Is.EqualTo(300));
            
            Access(entity1).Remove<Component1>();
            Assert.That(Access(entity1).Has<Component1>(), Is.False);
            
            var list = new List<int>() {1, 2, 3, 4};
            Access(entity1).Replace(new Component4(list));
            Assert.That(list.Count == 4, Is.True);
            Assert.That(Access(entity1).Get<Component4>().Value.Count == 4, Is.True);
            Access(entity1).Remove<Component4>();
            Assert.That(list.Count == 0, Is.True);
            
            Assert.That(Access(entity1).GetOrSet<Component5>().Value.Count, Is.EqualTo(0));
            Access(entity1).GetOrSet<Component5>().Value.Add(1);
            Assert.That(Access(entity1).GetOrSet<Component5>().Value.Count, Is.EqualTo(1));
            
            var entity1Copy = Access(entity1).Copy();
            Access(entity1Copy).GetOrSet<Component5>().Value.Add(2);
            
            Assert.That(Access(entity1).GetOrSet<Component5>().Value.Count, Is.EqualTo(1));
            Assert.That(Access(entity1Copy).GetOrSet<Component5>().Value.Count, Is.EqualTo(2));
            
            Access(entity1).Remove<Component5>();
            Assert.That(Access(entity1).Has<Component5>(), Is.False);
            Assert.That(Access(entity1Copy).GetOrSet<Component5>().Value.Count, Is.EqualTo(2));
            
            Assert.That(Access(entity1).GetOrSet<Component6>().Value.Count, Is.EqualTo(0));
            Access(entity1).GetOrSet<Component6>().Value.Add(1, 1);
            Assert.That(Access(entity1).GetOrSet<Component6>().Value.Count, Is.EqualTo(1));
            
            var entity1Copy2 = Access(entity1).Copy();
            Access(entity1Copy2).GetOrSet<Component6>().Value.Add(2, 2);
            
            Assert.That(Access(entity1).GetOrSet<Component6>().Value.Count, Is.EqualTo(1));
            Assert.That(Access(entity1Copy2).GetOrSet<Component6>().Value.Count, Is.EqualTo(2));
            
            Access(entity1).Remove<Component6>();
            Assert.That(Access(entity1).Has<Component6>(), Is.False);
            Assert.That(Access(entity1Copy2).GetOrSet<Component6>().Value.Count, Is.EqualTo(2));
            
            Access(entity1Copy2).Get<Component6>().Value.Remove(2);
            Assert.That(Access(entity1Copy2).GetOrSet<Component6>().Value.Count, Is.EqualTo(1));
            Assert.That(Access(entity1Copy2).GetOrSet<Component6>().Value.GetFirst(), Is.EqualTo(1));
            
            world.Dispose();
        }

        [Test]
        public void Run_ReplaceComponent_WhenMissing_AddsComponent() {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var entity = world.CreateEntity<IsTestEntity>();

            Assert.That(Access(entity).Has<Component1>(), Is.False);
            Assert.That(filter.GetCount(), Is.EqualTo(0));

            Access(entity).Replace(new Component1(42));

            Assert.That(Access(entity).Has<Component1>(), Is.True);
            Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(42));
            Assert.That(filter.GetCount(), Is.EqualTo(1));

            world.Dispose();
        }

        [Test]
        public void Run_ReplaceComponent_WhenPresent_UpdatesValue() {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var entity = world.CreateEntity<IsTestEntity>();
            Access(entity).Add(new Component1(10));

            Assert.That(filter.GetCount(), Is.EqualTo(1));
            Access(entity).Replace(new Component1(99));

            Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(99));
            Assert.That(filter.GetCount(), Is.EqualTo(1));

            world.Dispose();
        }

        [Test]
        public void Run_ReplaceComponent_DoesNotTouchOtherComponents() {
            var world = WorldBuilder.Build();
            var entity = world.CreateEntity<IsTestEntity>();
            Access(entity).Add(new Component1(1));
            Access(entity).Add(new Component3(2.5f));

            Access(entity).Replace(new Component1(100));

            Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(100));
            Assert.That(Access(entity).Get<Component3>().Value, Is.EqualTo(2.5f));

            world.Dispose();
        }

        [Test]
        public void Run_ReplaceComponent_AddThrowsDuplicate_ReplaceStillUpdates() {
            var world = WorldBuilder.Build();
            var entity = world.CreateEntity<IsTestEntity>();
            Access(entity).Replace(new Component1(1));
            Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(1));

            Assert.Throws<Exception>(() => Access(entity).Add(new Component1(2)));

            Access(entity).Replace(new Component1(3));
            Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(3));

            world.Dispose();
        }

        [Test]
        public void Run_ReplaceComponent_MultipleEntities_FilterSeesFreshValues() {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>());
            var e1 = world.CreateEntity<IsTestEntity>();
            var e2 = world.CreateEntity<IsTestEntity>();
            Access(e1).Add(new Component1(1));
            Access(e2).Add(new Component1(2));

            Access(e1).Replace(new Component1(10));
            Access(e2).Replace(new Component1(20));

            var sum = 0;
            foreach (var e in filter) {
                sum += Access(e).Get<Component1>().Value;
            }

            Assert.That(sum, Is.EqualTo(30));
            Assert.That(filter.GetCount(), Is.EqualTo(2));

            world.Dispose();
        }
        
        [Test]
        public void Run_Any_Remove_Components() {
            var world = WorldBuilder.Build();
            
            var entity1 = world.CreateEntity<IsTestEntity>();
            Access(entity1).Add(new Component1(1));
            var entity2 = world.CreateEntity<IsTestEntity>();
            Access(entity2).Add(new Component1(2));
            var entity3 = world.CreateEntity<IsTestEntity>();
            Access(entity3).Add(new Component1(3));
            
            Assert.That(Access(entity1).Get<Component1>().Value, Is.EqualTo(1));
            Assert.That(Access(entity2).Get<Component1>().Value, Is.EqualTo(2));
            Assert.That(Access(entity3).Get<Component1>().Value, Is.EqualTo(3));
            
            Access(entity2).Destroy();
            Assert.That(Access(entity1).Get<Component1>().Value, Is.EqualTo(1));
            Assert.That(Access(entity3).Get<Component1>().Value, Is.EqualTo(3));
            
            world.Dispose();
        }

        [Test]
        public void Run_Filters() {
            var world = WorldBuilder.Build();
            List<Entity> cacheEntities = new List<Entity>();
            var entity1 = world.CreateEntity<IsTestEntity>();
            Access(entity1).Add(new Component1(100));
            Access(entity1).Add(new Component2(true));
            Access(entity1).Add(new Component3(15.5f));
            Access(entity1).GetOrSet<IsTestEntity42>();
            
            var entity2 = world.CreateEntity<IsTestEntity>();
            Access(entity2).Add(new Component1(200));
            Access(entity2).Add(new Component3(0.5f));
            Access(entity1).GetOrSet<IsTestEntity29>();
            
            var entity3 = world.CreateEntity<IsTestEntity>();
            Access(entity3).Add(new Component1(50));
            Access(entity1).GetOrSet<IsTestEntity14>();
            
            var filter1 = world.GetFilter(new FilterMask<Component1>());
            Assert.That(filter1.GetCount(), Is.EqualTo(3));
            filter1.FillEntities(ref cacheEntities);
            Assert.That(cacheEntities[0] == entity1, Is.True);
            Assert.That(cacheEntities[1] == entity2, Is.True);
            Assert.That(cacheEntities[2] == entity3, Is.True);
            
            var filter2 = world.GetFilter(new FilterMask<Component1>.Exclude<Component3>());
            filter2.FillEntities(ref cacheEntities);
            Assert.That(filter2.GetCount(), Is.EqualTo(1));
            Assert.That(cacheEntities[0], Is.EqualTo(entity3));
            
            var filter3 = world.GetFilter(new FilterMask<Component1>.Exclude<Component2>());
            filter3.FillEntities(ref cacheEntities);
            Assert.That(filter3.GetCount(), Is.EqualTo(2));
            Assert.That(cacheEntities[0], Is.EqualTo(entity2));
            Assert.That(cacheEntities[1], Is.EqualTo(entity3));
            
            
            var filterMask12 = new FilterMask<Component1, Component2>();
            var filterMask21 = new FilterMask<Component2, Component1>();
            Assert.That(filterMask12.GetHashId(), Is.EqualTo(filterMask21.GetHashId()));
            
            var filter12 = world.GetFilter(filterMask12);
            var filter21 = world.GetFilter(filterMask21);
            Assert.That(filter12, Is.EqualTo(filter21));
            
            var filterMask12_3 = new FilterMask<Component1, Component2>.Exclude<Component3>();
            var filterMask21_3 = new FilterMask<Component2, Component1>.Exclude<Component3>();
            Assert.That(filterMask12_3.GetHashId(), Is.EqualTo(filterMask21_3.GetHashId()));
            
            var filterMask1_32 = new FilterMask<Component1>.Exclude<Component3, Component2>();
            var filterMask1_23 = new FilterMask<Component1>.Exclude<Component2, Component3>();
            Assert.That(filterMask1_32.GetHashId(), Is.EqualTo(filterMask1_23.GetHashId()));
            
            world.Dispose();
        }
        
        [Test]
        public void Run_Filter_UsesComponentStorageCompatibilityForDirtyEntities() {
            var world = WorldBuilder.Build();
            var filter = world.GetFilter(new FilterMask<Component1>.Exclude<Component2>());
            var entity = world.CreateEntity<IsTestEntity>();
            
            Access(entity).Add(new Component1(1));
            Assert.That(filter.GetCount(), Is.EqualTo(1));
            
            Access(entity).Add(new Component2(true));
            Assert.That(filter.GetCount(), Is.EqualTo(0));
            
            Access(entity).Remove<Component2>();
            Assert.That(filter.GetCount(), Is.EqualTo(1));
            
            Access(entity).Remove<Component1>();
            Assert.That(filter.GetCount(), Is.EqualTo(0));
            
            Access(entity).Replace(new Component1(2));
            Assert.That(filter.GetCount(), Is.EqualTo(1));
            
            Access(entity).Destroy();
            Assert.That(filter.GetCount(), Is.EqualTo(0));
            
            world.Dispose();
        }

        [Test]
        public void Run_Systems() {
            var world = WorldBuilder.Build();
            var systems = new Systems(world);
            systems
                .Add(new TestInitSystem())
                .Add(new TestUpdate1System())
                .Add(new TestUpdate2System())
                .Inject();
            
            systems.Init();
            var filter1 = world.GetFilter(new FilterMask<Component1>());
            Assert.That(filter1.GetCount(), Is.EqualTo(2));
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(1));
            }
            
            systems.Update();
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(2));
            }
            
            systems.Update();
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(3));
            }
            
            systems.Dispose();
            world.Dispose();
        }

        [Test]
        public void Run_RemoveOneTickSystems() {
            var world = WorldBuilder.Build();
            var systems = new Systems(world);
            systems
                .Add(new TestInitSystem())
                .Inject();
            
            systems.Init();
            
            var filter1 = world.GetFilter(new FilterMask<Component1>());
            foreach (var entity in filter1) {
                Access(entity).Replace(new Test1OneTick());
            }
            
            systems.Update();
            
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Has<Test1OneTick>(), Is.False);
            }
            
            foreach (var entity in filter1) {
                Access(entity).GetOrSet<Test1OneTick>();
            }
            
            var filter2 = world.GetFilter(new FilterMask<Component3>());
            foreach (var entity in filter2) {
                Access(entity).GetOrSet<Test1OneTick>();
                Access(entity).GetOrSet<Test2OneTick>();
            }
            
            systems.Update();
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Has<Test1OneTick>(), Is.False);
                Assert.That(Access(entity).Has<Test2OneTick>(), Is.False);
            }
            
            systems.Dispose();
            world.Dispose();
        }
        
        [Test]
        public void Run_GroupSystems() {
            var world = WorldBuilder.Build();
            var systems = new Systems(world);
            systems
                .Add(new TestInitSystem())
                .Add(new TestGroupSystems())
                .Inject();
            
            systems.Init();
            var filter1 = world.GetFilter(new FilterMask<Component1>());
            Assert.That(filter1.GetCount(), Is.EqualTo(2));
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(1));

                if (Access(entity).Has<Component3>()) {
                    Assert.That(Access(entity).Get<Component3>().Value, Is.EqualTo(0.5f));   
                }
            }
            
            systems.Update();
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(2));
                if (Access(entity).Has<Component3>()) {
                    Assert.That(Access(entity).Get<Component3>().Value, Is.EqualTo(1.5f));   
                }
            }
            
            systems.SetActiveGroup(nameof(TestSubGroupSystems), false);
            systems.Update();
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(3));
                if (Access(entity).Has<Component3>()) {
                    Assert.That(Access(entity).Get<Component3>().Value, Is.EqualTo(1.5f));   
                }
            }
            
            systems.SetActiveGroup(nameof(TestGroupSystems), false);
            systems.Update();
            foreach (var entity in filter1) {
                Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(3));
                if (Access(entity).Has<Component3>()) {
                    Assert.That(Access(entity).Get<Component3>().Value, Is.EqualTo(1.5f));   
                }
            }
            
            systems.Dispose();
            world.Dispose();
        }

        [Test]
        public void Run_ComponentInject_UpdatesComponentsAndFilters() {
            var world = WorldBuilder.Build();
            var component1Filter = world.GetFilter(new FilterMask<Component1>());
            var component2Filter = world.GetFilter(new FilterMask<Component2>());
            var component3Filter = world.GetFilter(new FilterMask<Component3>());
            var entity = world.CreateEntity<IsTestEntity>();
            var systems = new Systems(world);
            systems
                .Add(new TestComponentInjectSystem())
                .Inject();

            systems.Update();

            Assert.That(component1Filter.GetCount(), Is.EqualTo(1));
            Assert.That(component2Filter.GetCount(), Is.EqualTo(1));
            Assert.That(component3Filter.GetCount(), Is.EqualTo(1));
            Assert.That(Access(entity).Get<Component1>().Value, Is.EqualTo(2));
            Assert.That(Access(entity).Get<Component2>().Value, Is.True);
            Assert.That(Access(entity).Get<Component3>().Value, Is.EqualTo(4f));

            systems.Dispose();
            world.Dispose();
        }

        [Test]
        public void Run_Exceptions() {
            var world = WorldBuilder.Build();
            var entity1 = world.CreateEntity<IsTestEntity>();
            var entity2 = world.CreateEntity<IsTestEntity>();
            Assert.Throws<Exception>(() => {
                Access(entity1).Get<Component1>();
            });
            
            Access(entity1).Destroy();
            Assert.Throws<Exception>(() => {
                Access(entity1).Has<Component1>();
            });
            
            Assert.Throws<Exception>(() => {
                Access(entity1).GetOrSet<Component1>();
            });
            
            Assert.Throws<Exception>(() => {
                Access(entity1).Add(new Component1());
            });
            
            world.Dispose();
            Assert.Throws<Exception>(() => {
                Access(entity1).Has<Component1>();
            });
            
            Assert.Throws<Exception>(() => {
                Access(entity2).Has<Component1>();
            });
        }
        
        [OneTimeTearDown]
        public void Cleanup() { 
            WorldBuilder.AllDestroy();
        }
    }
    
    public struct IsTestEntity : IComponent { }
    public struct IsTestEntity1 : IComponent { }
    public struct IsTestEntity2 : IComponent { }
    public struct IsTestEntity3 : IComponent { }
    public struct IsTestEntity4 : IComponent { }
    public struct IsTestEntity5 : IComponent { }
    public struct IsTestEntity6 : IComponent { }
    public struct IsTestEntity7 : IComponent { }
    public struct IsTestEntity8 : IComponent { }
    public struct IsTestEntity9 : IComponent { }
    public struct IsTestEntity10 : IComponent { }
    public struct IsTestEntity11 : IComponent { }
    public struct IsTestEntity12 : IComponent { }
    public struct IsTestEntity13 : IComponent { }
    public struct IsTestEntity14 : IComponent { }
    public struct IsTestEntity15 : IComponent { }
    public struct IsTestEntity16 : IComponent { }
    public struct IsTestEntity17 : IComponent { }
    public struct IsTestEntity18 : IComponent { }
    public struct IsTestEntity19 : IComponent { }
    public struct IsTestEntity20 : IComponent { }
    public struct IsTestEntity21 : IComponent { }
    public struct IsTestEntity22 : IComponent { }
    public struct IsTestEntity23 : IComponent { }
    public struct IsTestEntity24 : IComponent { }
    public struct IsTestEntity25 : IComponent { }
    public struct IsTestEntity26 : IComponent { }
    public struct IsTestEntity27 : IComponent { }
    public struct IsTestEntity28 : IComponent { }
    public struct IsTestEntity29 : IComponent { }
    public struct IsTestEntity30 : IComponent { }
    public struct IsTestEntity31 : IComponent { }
    public struct IsTestEntity32 : IComponent { }
    public struct IsTestEntity33 : IComponent { }
    public struct IsTestEntity34 : IComponent { }
    public struct IsTestEntity35 : IComponent { }
    public struct IsTestEntity36 : IComponent { }
    public struct IsTestEntity37 : IComponent { }
    public struct IsTestEntity38 : IComponent { }
    public struct IsTestEntity39 : IComponent { }
    public struct IsTestEntity40 : IComponent { }
    public struct IsTestEntity41 : IComponent { }
    public struct IsTestEntity42 : IComponent { }
    public struct IsTestEntity43 : IComponent { }
    public struct IsTestEntity44 : IComponent { }
    public struct IsTestEntity45 : IComponent { }
    public struct Test1OneTick : IComponent, IOneTickComponent { }
    public struct Test2OneTick : IComponent, IOneTickComponent { }

    public struct Component1 : IComponent {
        public int Value;

        public Component1(int value) {
            Value = value;
        }
    }
    
    public struct Component2 : IComponent, IAutoResetComponent<Component2>, IAutoCopyComponent<Component2> {
        public bool Value;

        public Component2(bool value) {
            Value = value;
        }

        public void Reset(ref Component2 c) {
            c.Value = true;
        }

        public void Copy(ref Component2 src, ref Component2 dst) {
            dst.Value = src.Value;
        }
    }
    
    public struct Component3 : IComponent, IAutoCopyComponent<Component3> {
        public float Value;

        public Component3(float value) {
            Value = value;
        }

        public void Copy(ref Component3 src, ref Component3 dst) {
            dst.Value = src.Value;
        }
    }

    public struct Component4 : IComponent, IAutoDestroyComponent<Component4> {
        public List<int> Value;

        public Component4(List<int> value) {
            Value = value;
        }

        public void Destroy(ref Component4 c) {
            c.Value.Clear();
        }
    }

    public struct Component5 : IComponent, IAutoPoolComponent<Component5>, IAutoCopyComponent<Component5> {
        public PooledList<int> Value;
        
        public void Reset(ref Component5 c, IPoolFactory poolFactory) {
            c.Value = poolFactory.Rent<int>();
        }

        public void Copy(ref Component5 src, ref Component5 dst) {
            dst.Value = src.Value.Copy();
        }

        public void Destroy(ref Component5 c, IPoolFactory poolFactory) {
            c.Value.Return();
        }
    }
    
    public struct Component6 : IComponent, IAutoPoolComponent<Component6>, IAutoCopyComponent<Component6> {
        public PooledSparseArray<int> Value;
        
        public void Reset(ref Component6 c, IPoolFactory poolFactory) {
            c.Value = new PooledSparseArray<int>(4, poolFactory);
        }

        public void Copy(ref Component6 src, ref Component6 dst) {
            dst.Value = new PooledSparseArray<int>(src.Value);
        }

        public void Destroy(ref Component6 c, IPoolFactory poolFactory) {
            c.Value.Dispose();
        }
    }

    public sealed class TestInitSystem : IInitSystem {
        private readonly WorldInject _worldInject = default;
        private readonly ComponentInject<Component1> _component1 = default;
        private readonly ComponentInject<Component3> _component3 = default;
        
        public void Init() {
            var entity1 = _worldInject.Value.CreateEntity<IsTestEntity>();
            _component1.Add(entity1, new Component1(1));
            
            var entity2 = _worldInject.Value.CreateEntity<IsTestEntity>();
            _component1.Add(entity2, new Component1(1));
            _component3.Add(entity2, new Component3(0.5f));
        }
    }
    
    public sealed class TestUpdate1System: IUpdateSystem {
        private readonly FilterInject<Include<Component1>> _filter = default;
        private readonly ComponentInject<Component1> _component1 = default;

        public void Update() {
            foreach (var entity in _filter.Value) {
                ref var component = ref _component1.Get(entity);
                component.Value += 1;
            }
        }
    }
    
    public sealed class TestUpdate2System : IUpdateSystem {
        private readonly FilterInject<Include<Component1>, Exclude<Component2>> _filter = default;
        private readonly ComponentInject<Component2> _component2 = default;

        public void Update() {
            foreach (var entity in _filter.Value) {
                _component2.Add(entity, new Component2());   
            }
        }
    }
    
    public sealed class TestUpdate3System : IUpdateSystem {
        private readonly FilterInject<Include<Component3>> _filter = default;
        private readonly ComponentInject<Component3> _component3 = default;

        public void Update() {
            foreach (var entity in _filter.Value) {
                ref var c = ref _component3.Get(entity);
                c.Value += 1;
            }
        }
    }

    public sealed class TestComponentInjectSystem : IUpdateSystem {
        private readonly FilterInject<Include<IsTestEntity>> _entities = default;
        private readonly ComponentInject<Component1> _component1 = default;
        private readonly ComponentInject<Component2> _component2 = default;
        private readonly ComponentInject<Component3> _component3 = default;

        public void Update() {
            foreach (var entity in _entities.Value) {
                if (!_component1.Has(entity)) {
                    ref Component1 component1 = ref _component1.Set(entity);
                    component1.Value = 1;
                }

                ref Component1 existingComponent1 = ref _component1.Get(entity);
                existingComponent1.Value++;

                _component2.Add(entity, new Component2(false));
                _component2.Replace(entity, new Component2(true));

                ref Component3 component3 = ref _component3.GetOrSet(entity);
                component3.Value = 4f;
            }
        }
    }

    public sealed class TestGroupSystems : IGroupSystem {
        public string GroupName => nameof(TestGroupSystems);
        public bool State => true;

        public ISystem[] Systems => new ISystem[] {
            new TestUpdate1System(),
            new TestSubGroupSystems(),
        };
    }

    public sealed class TestSubGroupSystems : IGroupSystem {
        public string GroupName => nameof(TestSubGroupSystems);
        public bool State => true;
        public ISystem[] Systems => new ISystem[] {
            new TestUpdate3System(),
        };
    }
}
