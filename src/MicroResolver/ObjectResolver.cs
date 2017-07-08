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

        /// <summary>
        /// Verify and Compile container.
        /// </summary>
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
            lifestyleByType[typeof(TInterface)] = lifestyle;
        }

        public void RegisterCollection<TInterface>(Lifestyle lifestyle, params Type[] implementationTypes)
            where TInterface : class
        {
            if (isCompiled) throw new Exception("todo message");

            compilation.AddCollection(typeof(TInterface), implementationTypes, lifestyle);
            lifestyleByType[typeof(TInterface)] = lifestyle;
        }

        public abstract T Resolve<T>();
        protected abstract void SetFactory<T>(Func<T> factory);
    }
}