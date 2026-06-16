namespace Pixagen.Ecs.Runtime {
    public readonly struct Options {
        public readonly uint SystemsCapacity;
        public readonly uint EntityCapacity;
        public readonly uint FilterCapacity;
        public readonly uint ComponentCapacity;
        public readonly uint ComponentEntityCapacity;
        
        public static Options Default = new Options(256, 1024, 512, 512, 128);

        public Options(uint systemsCapacity, uint entityCapacity, uint filterCapacity, uint componentCapacity, uint componentEntityCapacity) {
            SystemsCapacity = systemsCapacity;
            EntityCapacity = entityCapacity;
            FilterCapacity = filterCapacity;
            ComponentCapacity = componentCapacity;
            ComponentEntityCapacity = componentEntityCapacity;
        }
    }   
}
