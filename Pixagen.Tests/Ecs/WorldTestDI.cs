using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using Pixagen.Ecs.DI;
using static Pixagen.Tests.TestSupport.EcsTestAccess;

namespace Pixagen.Tests.Ecs
{
    [TestFixture]
    public class WorldTestDI
    {
        [Test]
        public void Run_DI()
        {
            var world = WorldBuilder.Build();
            var systems = new Systems(world);
            systems
                .Add(new TestInjectSystem())
                .Inject(new TestData());

            CreateFilterEntities(world);

            systems.Update();

            systems.Dispose();
            world.Dispose();
        }

        [Test]
        public void Run_GroupInjects()
        {
            var world = WorldBuilder.Build();
            var systems = new Systems(world);
            systems
                .Add(new TestInjectGroup())
                .Inject();

            CreateFilterEntities(world);

            systems.Update();

            systems.Dispose();
            world.Dispose();
        }

        [Test]
        public void Run_GroupHelperReceivesDiAndIsInjectedIntoSystem()
        {
            var world = WorldBuilder.Build();
            var systems = new Systems(world);
            var group = new TestHelperGroup();
            systems
                .Add(group)
                .Inject();

            systems.Init();

            Assert.That(group.Helper.AfterInjected, Is.True);
            Assert.That(group.Helper.Created, Is.Not.EqualTo(Entity.Empty));
            Assert.That(Access(group.Helper.Created).Has<Component1>(), Is.True);
            Assert.That(Access(group.Helper.Created).Get<Component1>().Value, Is.EqualTo(11));

            systems.Dispose();
            world.Dispose();
        }

        [Test]
        public void Run_PlainServiceFieldIsNotInjected()
        {
            var world = WorldBuilder.Build();
            var systems = new Systems(world);
            var system = new TestPlainServiceFieldSystem();
            systems
                .Add(system)
                .Inject(new TestData());

            systems.Init();

            Assert.That(system.WasInjected, Is.False);

            systems.Dispose();
            world.Dispose();
        }

        [Test]
        public void Run_GroupDisposeInjectsOnSystemsDispose()
        {
            var world = WorldBuilder.Build();
            var systems = new Systems(world);
            var group = new TestDisposeGroup();
            systems
                .Add(group)
                .Inject();

            systems.Dispose();

            Assert.That(group.DisposeInject.Disposed, Is.True);
            world.Dispose();
        }

        private static void CreateFilterEntities(IWorld world)
        {
            var entity1 = world.CreateEntity<IsTestEntity>();
            Access(entity1).GetOrSet<Component1>();
            Access(entity1).GetOrSet<Component2>();
            Access(entity1).GetOrSet<Component3>();

            var entity2 = world.CreateEntity<IsTestEntity>();
            Access(entity2).GetOrSet<Component1>();
            Access(entity2).GetOrSet<Component2>();

            var entity3 = world.CreateEntity<IsTestEntity>();
            Access(entity3).GetOrSet<Component1>();
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            WorldBuilder.AllDestroy();
        }
    }

    public sealed class TestInjectSystem : IUpdateSystem
    {
        private readonly WorldInject _world = default;
        private readonly FilterInject<Include<Component1, Component2>, Exclude<Component3>> _filterInject = default;
        private readonly SystemsInject _systemsInject = default;
        private readonly CustomInject<TestData> _testData = default;

        public void Update()
        {
            Assert.That(_world.Value, Is.Not.Null);
            Assert.That(_filterInject.Value, Is.Not.Null);
            Assert.That(_filterInject.Value.GetCount(), Is.EqualTo(1));
            Assert.That(_testData.Value, Is.Not.Null);
            Assert.That(_systemsInject.Value, Is.Not.Null);
        }
    }

    public sealed class TestInjectGroup : IGroupSystem
    {
        public string GroupName => nameof(TestInjectGroup);
        public bool State => true;
        public object[] Injects => [new TestData()];
        public ISystem[] Systems { get; } = [new TestInjectSystem()];
    }

    public sealed class TestHelperGroup : IGroupSystem
    {
        public TestDiHelper Helper { get; } = new TestDiHelper();
        private readonly TestData _data = new TestData { Value1 = 11 };
        public string GroupName => nameof(TestHelperGroup);
        public bool State => true;
        public object[] Injects => [_data, Helper];
        public ISystem[] Systems { get; } = [new TestHelperSystem()];
    }

    public sealed class TestDiHelper : IAfterInject
    {
        private readonly WorldInject _world = default;
        private readonly ComponentInject<Component1> _components = default;
        private readonly CustomInject<TestData> _data = default;

        public bool AfterInjected { get; private set; }
        public Entity Created { get; private set; }

        public void AfterInject()
        {
            AfterInjected = _world.Value is not null && _data.Value is not null;
        }

        public Entity Create()
        {
            Created = _world.Create<IsTestEntity>();
            _components.Add(Created, new Component1(_data.Value.Value1));
            return Created;
        }
    }

    public sealed class TestHelperSystem : IInitSystem
    {
        private readonly CustomInject<TestDiHelper> _helper = default;

        public void Init()
        {
            _helper.Value.Create();
        }
    }

    public sealed class TestPlainServiceFieldSystem : IInitSystem
    {
        private readonly TestData _data = default!;

        public bool WasInjected { get; private set; }

        public void Init()
        {
            WasInjected = _data is not null;
        }
    }

    public sealed class TestDisposeGroup : IGroupSystem
    {
        public TestDisposeInject DisposeInject { get; } = new TestDisposeInject();
        public string GroupName => nameof(TestDisposeGroup);
        public bool State => true;
        public object[] Injects => [DisposeInject];
        public ISystem[] Systems { get; } = [];
    }

    public sealed class TestDisposeInject : IDisposeInject
    {
        public bool Disposed { get; private set; }

        public void DisposeInject()
        {
            Disposed = true;
        }
    }

    public class TestData
    {
        public int Value1;
        public float Value2;
        public string Value3 = string.Empty;
    }
}
