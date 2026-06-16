namespace Pixagen.Ecs.DI
{
    public struct CustomInject<T> : ICustomDataInject where T : class
    {
        private T _value;

        public readonly T Value => _value;

        public CustomInject(T value)
        {
            _value = value;
        }

        void ICustomDataInject.Fill(object[] injects)
        {
            if (injects.Length > 0)
            {
                var vType = typeof(T);
                foreach (var inject in injects)
                {
                    if (vType.IsInstanceOfType(inject))
                    {
                        _value = (T)inject;
                        break;
                    }
                }
            }
        }
    }
}
