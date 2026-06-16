using System;

namespace Pixagen.Ecs.Runtime {
    public interface IGroupSystem : ISystem {
        public string GroupName { get; }
        public bool State { get; }
        public object[] Injects => Array.Empty<object>();
        public ISystem[] Systems { get; }
    }   
}
