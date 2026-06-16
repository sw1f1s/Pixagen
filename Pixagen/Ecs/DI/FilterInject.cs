namespace Pixagen.Ecs.DI
{
    public struct FilterInject<Inc> : IDataInject
        where Inc : struct, IInclude
    {
        private Filter _value;

        public readonly Filter Value => _value;

        void IDataInject.Fill(ISystems systems)
        {
            using var mask1 = default(Inc).GetMask();
            _value = systems.World.GetFilter(mask1);
        }
    }

    public struct FilterInject<Inc, Exc> : IDataInject
        where Inc : struct, IInclude
        where Exc : struct, IExclude
    {
        private Filter _value;

        public readonly Filter Value => _value;

        void IDataInject.Fill(ISystems systems)
        {
            using var mask1 = default(Inc).GetMask();
            using var mask2 = default(Exc).GetMask();
            _value = systems.World.GetFilter(FilterMask.Combine(mask1, mask2));
        }
    }

    public struct Include<Inc1> : IInclude
        where Inc1 : struct, IComponent
    {
        FilterMask IInclude.GetMask()
        {
            return new FilterMask<Inc1>();
        }
    }

    public struct Include<Inc1, Inc2> : IInclude
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
    {
        FilterMask IInclude.GetMask()
        {
            return new FilterMask<Inc1, Inc2>();
        }
    }

    public struct Include<Inc1, Inc2, Inc3> : IInclude
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
        where Inc3 : struct, IComponent
    {
        FilterMask IInclude.GetMask()
        {
            return new FilterMask<Inc1, Inc2, Inc3>();
        }
    }

    public struct Include<Inc1, Inc2, Inc3, Inc4> : IInclude
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
        where Inc3 : struct, IComponent
        where Inc4 : struct, IComponent
    {
        FilterMask IInclude.GetMask()
        {
            return new FilterMask<Inc1, Inc2, Inc3, Inc4>();
        }
    }

    public struct Include<Inc1, Inc2, Inc3, Inc4, Inc5> : IInclude
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
        where Inc3 : struct, IComponent
        where Inc4 : struct, IComponent
        where Inc5 : struct, IComponent
    {
        FilterMask IInclude.GetMask()
        {
            return new FilterMask<Inc1, Inc2, Inc3, Inc4, Inc5>();
        }
    }

    public struct Include<Inc1, Inc2, Inc3, Inc4, Inc5, Inc6> : IInclude
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
        where Inc3 : struct, IComponent
        where Inc4 : struct, IComponent
        where Inc5 : struct, IComponent
        where Inc6 : struct, IComponent
    {
        FilterMask IInclude.GetMask()
        {
            return new FilterMask<Inc1, Inc2, Inc3, Inc4, Inc5, Inc6>();
        }
    }

    public struct Exclude<Exc1> : IExclude
        where Exc1 : struct, IComponent
    {
        FilterMask IExclude.GetMask()
        {
            return new FilterMaskExclude<Exc1>();
        }
    }

    public struct Exclude<Exc1, Exc2> : IExclude
        where Exc1 : struct, IComponent
        where Exc2 : struct, IComponent
    {
        FilterMask IExclude.GetMask()
        {
            return new FilterMaskExclude<Exc1, Exc2>();
        }
    }
}
