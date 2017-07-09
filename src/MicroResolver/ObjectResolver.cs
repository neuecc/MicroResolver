using System;
using System.Linq;
using System.Collections.Generic;
using MicroResolver.Internal;
using System.Reflection;

namespace MicroResolver
{
    public abstract partial class ObjectResolver : IObjectResolver
    {
        CompilationContext compilation;
        bool isCompiled;
        FixedTypeKeyHashtable<Func<object>> nongenericResolversTable;
        FixedTypeKeyHashtable<Lifestyle> lifestyleByType;

        public ObjectResolver()
        {
            compilation = new CompilationContext(this);
        }

        /// <summary>
        /// Verify and Compile container.
        /// </summary>
        public void Compile()
        {
            if (isCompiled) throw new MicroResolverException("Already compiled, can not compile twice.");

            isCompiled = true;
            var registeredTypes = compilation.Compile();
            CreateResolverHashTable(registeredTypes);
            CreateLifestyleTypeHashTable(registeredTypes);
            compilation = null;
        }

#if DEBUG && NET_45

        public System.Reflection.Emit.AssemblyBuilder DebuggingCompile()
        {
            return compilation.DebuggingCompile("DynamicCompilation");
        }

#endif

        void CreateResolverHashTable(IMeta[] registeredTypes)
        {
            var resolveMethod = typeof(ObjectResolver).GetRuntimeMethods().First(x => x.Name == "Resolve" && x.IsGenericMethod);

            var prepare = registeredTypes
                .Select(item =>
                {
                    var resolver = (Func<object>)resolveMethod.MakeGenericMethod(item.InterfaceType).CreateDelegate(typeof(Func<object>), this);
                    return new KeyValuePair<Type, Func<object>>(item.InterfaceType, resolver);
                })
                .ToArray();

            nongenericResolversTable = new FixedTypeKeyHashtable<Func<object>>(prepare);
        }

        void CreateLifestyleTypeHashTable(IMeta[] registeredTypes)
        {
            var prepare = registeredTypes.
                Select(item =>
                {
                    return new KeyValuePair<Type, Lifestyle>(item.InterfaceType, item.Lifestyle);
                })
                .ToArray();
            lifestyleByType = new FixedTypeKeyHashtable<Lifestyle>(prepare);
        }

        public IScopedObjectResolver BeginScope(ScopeProvider provider)
        {
            provider.Initialize(this);
            var scope = provider.Create(this);
            return scope;
        }

        public Lifestyle Lifestyle(Type type)
        {
            if (!isCompiled) throw new MicroResolverException("Not yet compiled, you can access only after compile.");

            return lifestyleByType.Get(type);
        }

        public void Register<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface
        {
            Register<TInterface, TImplementation>(MicroResolver.Lifestyle.Transient);
        }

        public void Register<TInterface, TImplementation>(Lifestyle lifestyle)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            Register(lifestyle, typeof(TInterface), typeof(TImplementation));
        }

        public void Register(Lifestyle lifestyle, Type interfaceType, Type implementationType)
        {
            if (isCompiled) throw new MicroResolverException("Already compiled, can not register new type when compile finished.");
            compilation.Add(interfaceType, implementationType, lifestyle);
        }

        public void RegisterCollection<TInterface>(params Type[] implementationTypes)
            where TInterface : class
        {
            RegisterCollection<TInterface>(MicroResolver.Lifestyle.Transient, implementationTypes);
        }

        public void RegisterCollection<TInterface>(Lifestyle lifestyle, params Type[] implementationTypes)
            where TInterface : class
        {
            RegisterCollection(lifestyle, typeof(TInterface), implementationTypes);
        }

        public void RegisterCollection(Lifestyle lifestyle, Type interfaceType, Type[] implementationTypes)
        {
            compilation.AddCollection(interfaceType, implementationTypes, lifestyle);
        }

        public object Resolve(Type type)
        {
            return nongenericResolversTable.Get(type).Invoke();
        }

        public abstract T Resolve<T>();
        protected abstract void SetFactory<T>(Func<T> factory);
    }
}