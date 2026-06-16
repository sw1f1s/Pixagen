namespace Pixagen.Ecs.Runtime {
    public abstract class FilterThreadJob {
        public void Execute(Filter filter) {
            filter.UpdateIfDirty();
            for (int i = 0; i < filter.Entities.Count; i++) {
                ExecuteInternal(filter.Entities.DenseItems[i].Value);
            }
        }

        protected abstract void ExecuteInternal(Entity entity);
    }
}
