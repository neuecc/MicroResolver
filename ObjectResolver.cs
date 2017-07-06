using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MicroResolver
{
    public abstract partial class ObjectResolver : IObjectResolver
    {
        public IObjectResolver BeginScope(ScopeProvider provider)
        {
            return provider.Create(this);
        }

        public LifestyleType LifestyleType<T>()
        {
            return (LifestyleType)GetCachedFactory<T>().lifestyle;
        }

        public T Resolve<T>()
        {
            var f = GetCachedFactory<T>();
            switch (f.lifestyle)
            {
                case Lifestyle.Transient:
                case Lifestyle.Scoped:
                    return f.factory.Invoke();
                case Lifestyle.Singleton:
                    return f.singleton.Value;
                default:
                    throw new Exception("Can't resolve type:" + typeof(T).Name);
            }
        }

        public bool TryResolve<T>(out T instance)
        {
            var f = GetCachedFactory<T>();
            switch (f.lifestyle)
            {
                case Lifestyle.Transient:
                case Lifestyle.Scoped:
                    instance = f.factory.Invoke();
                    return true;
                case Lifestyle.Singleton:
                    instance = f.singleton.Value;
                    return true;
                default:
                    instance = default(T);
                    return false; // not registered
            }
        }

        public bool TryResolveMany<T>(out IEnumerable<T> instances) where T : class
        {
            var f = GetCachedManyFactory<T>();
            switch (f.lifestyle)
            {
                case Lifestyle.Transient:
                case Lifestyle.Scoped:
                    instances = f.factory.Invoke();
                    return true;
                case Lifestyle.Singleton:
                    instances = f.singleton.Value;
                    return true;
                default:
                    instances = default(IEnumerable<T>);
                    return false; // not registered
            }
        }

        public void Register<T>(Lifestyle lifestyle, Func<T> factory)
        {
            switch (lifestyle)
            {
                case Lifestyle.Transient:
                case Lifestyle.Scoped:
                    SetCachedFactory(new CachedFactory<T>(lifestyle, factory, null));
                    break;
                case Lifestyle.Singleton:
                    SetCachedFactory(new CachedFactory<T>(lifestyle, null, new Lazy<T>(factory)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifestyle) + ": " + lifestyle);
            }
        }

        public void RegisterMany<T>(Lifestyle lifestyle, params Func<T>[] factories) where T : class
        {
            switch (lifestyle)
            {
                case Lifestyle.Transient:
                case Lifestyle.Scoped:
                    SetCachedManyFactory(new CachedFactory<IEnumerable<T>>(lifestyle, () => factories.Select(x => x()), null));
                    break;
                case Lifestyle.Singleton:
                    SetCachedManyFactory(new CachedFactory<IEnumerable<T>>(lifestyle, null, new Lazy<IEnumerable<T>>(() => factories.Select(x => x()).ToArray())));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifestyle) + ": " + lifestyle);
            }
        }

        protected abstract ref CachedFactory<T> GetCachedFactory<T>();
        protected abstract void SetCachedFactory<T>(CachedFactory<T> factory);
        protected abstract ref CachedFactory<IEnumerable<T>> GetCachedManyFactory<T>();
        protected abstract void SetCachedManyFactory<T>(CachedFactory<IEnumerable<T>> factory);
    }

    public struct CachedFactory<T>
    {
        public Lifestyle lifestyle;
        public Func<T> factory;
        public Lazy<T> singleton;

        public CachedFactory(Lifestyle lifestyle, Func<T> factory, Lazy<T> lazyFactory)
        {
            this.lifestyle = lifestyle;
            this.factory = factory;
            this.singleton = lazyFactory;
        }
    }
}
