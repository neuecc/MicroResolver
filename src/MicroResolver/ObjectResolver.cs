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
        FixedTypeKeyHashtable<Func<object>>.HashTuple[][] nongenericResolversTable;
        int hashIndexForNonGenericResolversTable;
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
                    var resolver = (Func<object>)item.EmittedDelegate;
                    return new KeyValuePair<Type, Func<object>>(item.InterfaceType, resolver);
                })
                .ToArray();

            nongenericResolversTable = new FixedTypeKeyHashtable<Func<object>>(prepare, 0.12f).table; // mod load factor
            hashIndexForNonGenericResolversTable = nongenericResolversTable.Length - 1;

            // fill empty for reduce null check
            for (int i = 0; i < nongenericResolversTable.Length; i++)
            {
                if (nongenericResolversTable[i] == null)
                {
                    nongenericResolversTable[i] = NoType.SingleHashArray;
                }
            }
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
            var hashCode = type.GetHashCode();
            var buckets = nongenericResolversTable[hashCode & hashIndexForNonGenericResolversTable];

            // optimize for single case
            if (buckets[0].type == type)
            {
                return buckets[0].value.Invoke();
            }

            for (int i = 1; i < buckets.Length; i++)
            {
                if (buckets[i].type == type)
                {
                    return buckets[i].value.Invoke();
                }
            }

            ERROR:
            throw new MicroResolverException("Type was not dound, Type: " + type.FullName);
        }

        public abstract T Resolve<T>();
        protected abstract void SetFactory<T>(Func<T> factory);
    }

    internal class NoType
    {
        internal static readonly FixedTypeKeyHashtable<Func<object>>.HashTuple[]
            SingleHashArray = new[] { new FixedTypeKeyHashtable<Func<object>>.HashTuple() { type = typeof(NoType) } };
    }
}