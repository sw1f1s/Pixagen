using System;
using Pixagen.Ecs.Collections;

namespace Pixagen.Ecs.Runtime
{
    public class FilterMask : IDisposable
    {
        protected int _mainComponent;
        protected BitMask _includes;
        protected BitMask _excludes;

        internal int MainComponent => _mainComponent;

        protected FilterMask()
        {
            _mainComponent = -1;
            _includes = new BitMask((int)Options.Default.ComponentEntityCapacity);
            _excludes = new BitMask((int)Options.Default.ComponentEntityCapacity);
        }

        internal BitMask GetIncludes()
        {
            return _includes.Clone();
        }

        internal BitMask GetExcludes()
        {
            return _excludes.Clone();
        }

        public int GetHashId()
        {
            return _includes.Hash ^ _excludes.Hash;
        }

        public static FilterMask Combine(FilterMask mask1, FilterMask mask2)
        {
            var mask = new FilterMask();
            mask._mainComponent = mask1.MainComponent >= 0 ? mask1.MainComponent : mask2.MainComponent;
            mask._includes = mask1._includes | mask2._includes;
            mask._excludes = mask1._excludes | mask2._excludes;
            return mask;
        }

        ~FilterMask() =>
            Dispose();

        public void Dispose()
        {
            _includes.Dispose();
            _excludes.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class FilterMaskExclude<Exc1> : FilterMask
        where Exc1 : struct, IComponent
    {
        public FilterMaskExclude()
        {
            _mainComponent = ComponentStorageIndex<Exc1>.StaticId;
            _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
        }
    }

    public class FilterMaskExclude<Exc1, Exc2> : FilterMask
        where Exc1 : struct, IComponent
        where Exc2 : struct, IComponent
    {

        public FilterMaskExclude()
        {
            _mainComponent = ComponentStorageIndex<Exc1>.StaticId;
            _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
            _excludes.Set(ComponentStorageIndex<Exc2>.StaticId);
        }
    }

    public class FilterMaskExclude<Exc1, Exc2, Exc3> : FilterMask
        where Exc1 : struct, IComponent
        where Exc2 : struct, IComponent
        where Exc3 : struct, IComponent
    {
        public FilterMaskExclude()
        {
            _mainComponent = ComponentStorageIndex<Exc1>.StaticId;
            _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
            _excludes.Set(ComponentStorageIndex<Exc2>.StaticId);
            _excludes.Set(ComponentStorageIndex<Exc3>.StaticId);
        }
    }

    public class FilterMask<Inc1> : FilterMask
            where Inc1 : struct, IComponent
    {

        public FilterMask() : base()
        {
            _mainComponent = ComponentStorageIndex<Inc1>.StaticId;
            _includes.Set(ComponentStorageIndex<Inc1>.StaticId);
        }

        public class Exclude<Exc1> : FilterMask<Inc1>
            where Exc1 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
            }
        }

        public class Exclude<Exc1, Exc2> : FilterMask<Inc1>
            where Exc1 : struct, IComponent
            where Exc2 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
                _excludes.Set(ComponentStorageIndex<Exc2>.StaticId);
            }
        }
    }

    public class FilterMask<Inc1, Inc2> : FilterMask
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
    {
        public FilterMask() : base()
        {
            _mainComponent = ComponentStorageIndex<Inc1>.StaticId;
            _includes.Set(ComponentStorageIndex<Inc1>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc2>.StaticId);
        }

        public class Exclude<Exc1> : FilterMask<Inc1, Inc2>
            where Exc1 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
            }
        }

        public class Exclude<Exc1, Exc2> : FilterMask<Inc1, Inc2>
            where Exc1 : struct, IComponent
            where Exc2 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
                _excludes.Set(ComponentStorageIndex<Exc2>.StaticId);
            }
        }
    }

    public class FilterMask<Inc1, Inc2, Inc3> : FilterMask
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
        where Inc3 : struct, IComponent
    {
        public FilterMask() : base()
        {
            _mainComponent = ComponentStorageIndex<Inc1>.StaticId;
            _includes.Set(ComponentStorageIndex<Inc1>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc2>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc3>.StaticId);
        }

        public class Exclude<Exc1> : FilterMask<Inc1, Inc2, Inc3>
            where Exc1 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
            }
        }

        public class Exclude<Exc1, Exc2> : FilterMask<Inc1, Inc2, Inc3>
            where Exc1 : struct, IComponent
            where Exc2 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
                _excludes.Set(ComponentStorageIndex<Exc2>.StaticId);
            }
        }
    }

    public class FilterMask<Inc1, Inc2, Inc3, Inc4> : FilterMask
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
        where Inc3 : struct, IComponent
        where Inc4 : struct, IComponent
    {
        public FilterMask() : base()
        {
            _mainComponent = ComponentStorageIndex<Inc1>.StaticId;
            _includes.Set(ComponentStorageIndex<Inc1>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc2>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc3>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc4>.StaticId);
        }

        public class Exclude<Exc1> : FilterMask<Inc1, Inc2, Inc3, Inc4>
            where Exc1 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
            }
        }

        public class Exclude<Exc1, Exc2> : FilterMask<Inc1, Inc2, Inc3, Inc4>
            where Exc1 : struct, IComponent
            where Exc2 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
                _excludes.Set(ComponentStorageIndex<Exc2>.StaticId);
            }
        }
    }

    public class FilterMask<Inc1, Inc2, Inc3, Inc4, Inc5> : FilterMask
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
        where Inc3 : struct, IComponent
        where Inc4 : struct, IComponent
        where Inc5 : struct, IComponent
    {
        public FilterMask() : base()
        {
            _mainComponent = ComponentStorageIndex<Inc1>.StaticId;
            _includes.Set(ComponentStorageIndex<Inc1>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc2>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc3>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc4>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc5>.StaticId);
        }

        public class Exclude<Exc1> : FilterMask<Inc1, Inc2, Inc3, Inc4, Inc5>
            where Exc1 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
            }
        }

        public class Exclude<Exc1, Exc2> : FilterMask<Inc1, Inc2, Inc3, Inc4, Inc5>
            where Exc1 : struct, IComponent
            where Exc2 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
                _excludes.Set(ComponentStorageIndex<Exc2>.StaticId);
            }
        }
    }

    public class FilterMask<Inc1, Inc2, Inc3, Inc4, Inc5, Inc6> : FilterMask
        where Inc1 : struct, IComponent
        where Inc2 : struct, IComponent
        where Inc3 : struct, IComponent
        where Inc4 : struct, IComponent
        where Inc5 : struct, IComponent
        where Inc6 : struct, IComponent
    {
        public FilterMask() : base()
        {
            _mainComponent = ComponentStorageIndex<Inc1>.StaticId;
            _includes.Set(ComponentStorageIndex<Inc1>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc2>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc3>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc4>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc5>.StaticId);
            _includes.Set(ComponentStorageIndex<Inc6>.StaticId);
        }

        public class Exclude<Exc1> : FilterMask<Inc1, Inc2, Inc3, Inc4, Inc5, Inc6>
            where Exc1 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
            }
        }

        public class Exclude<Exc1, Exc2> : FilterMask<Inc1, Inc2, Inc3, Inc4, Inc5, Inc6>
            where Exc1 : struct, IComponent
            where Exc2 : struct, IComponent
        {
            public Exclude() : base()
            {
                _excludes.Set(ComponentStorageIndex<Exc1>.StaticId);
                _excludes.Set(ComponentStorageIndex<Exc2>.StaticId);
            }
        }
    }
}
