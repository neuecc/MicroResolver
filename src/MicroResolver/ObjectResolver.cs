using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MicroResolver.Internal;

namespace MicroResolver
{
    public abstract partial class ObjectResolver : IObjectResolver
    {
        CompilationContext compilation;
        Dictionary<Type, Lifestyle> lifestyleByType;
        bool isCompiled;

        public ObjectResolver()
        {
            compilation = new CompilationContext(this);
            lifestyleByType = new Dictionary<Type, Lifestyle>();
        }

        public void Compile()
        {
            if (isCompiled) throw new Exception("todo message");

            isCompiled = true;
            compilation.Compile();
            compilation = null;
        }

#if DEBUG && NET_45

        public System.Reflection.Emit.AssemblyBuilder DebuggingCompile()
        {
            return compilation.DebuggingCompile("DynamicCompilation");
        }

#endif


        public IObjectResolver BeginScope(ScopeProvider provider)
        {
            return provider.Create(this);
        }

        public LifestyleType LifestyleType<T>()
        {
            Lifestyle v;
            if (lifestyleByType.TryGetValue(typeof(T), out v))
            {
                return (LifestyleType)v;
            }
            else
            {
                return MicroResolver.LifestyleType.NotRegistered;
            }
        }

        public void Register<TInterface, TImplementation>(Lifestyle lifestyle)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            if (isCompiled) throw new Exception("todo message");

            compilation.Add(typeof(TInterface), typeof(TImplementation), lifestyle);
            lifestyleByType.Add(typeof(TInterface), lifestyle);
        }

        //public void RegisterMany<T>(Lifestyle lifestyle, params Func<T>[] factories) where T : class
        //{
        //    switch (lifestyle)
        //    {
        //        case Lifestyle.Transient:
        //        case Lifestyle.Scoped:
        //            SetCachedManyFactory(new CachedFactory<IEnumerable<T>>(lifestyle, () => factories.Select(x => x()), null));
        //            break;
        //        case Lifestyle.Singleton:
        //            SetCachedManyFactory(new CachedFactory<IEnumerable<T>>(lifestyle, null, new Lazy<IEnumerable<T>>(() => factories.Select(x => x()).ToArray())));
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(lifestyle) + ": " + lifestyle);
        //    }
        //}

        public abstract T Resolve<T>();
        protected abstract void SetFactory<T>(Func<T> factory);
    }
}