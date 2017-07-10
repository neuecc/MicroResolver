using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MicroResolver.Internal
{
    internal class CompilationContext
    {
        Dictionary<Type, IMeta> registerdTypes = new Dictionary<Type, IMeta>();
        Stack<Type> circularReferenceChecker = new Stack<Type>();

        public ObjectResolver Resolver { get; }

        public CompilationContext(ObjectResolver resolver)
        {
            this.Resolver = resolver;
        }

        public void Add(Type interfaceType, Type implementationType, Lifestyle lifestyle)
        {
            registerdTypes[interfaceType] = new Meta(interfaceType, implementationType, lifestyle);
        }

        public void AddCollection(Type interfaceType, Type[] implementationType, Lifestyle lifestyle)
        {
            registerdTypes[typeof(IEnumerable<>).MakeGenericType(interfaceType)] = new CollectionMeta(interfaceType, typeof(IEnumerable<>).MakeGenericType(interfaceType), implementationType, lifestyle);
            registerdTypes[typeof(IReadOnlyList<>).MakeGenericType(interfaceType)] = new CollectionMeta(interfaceType, typeof(IReadOnlyList<>).MakeGenericType(interfaceType), implementationType, lifestyle);
            registerdTypes[interfaceType.MakeArrayType()] = new CollectionMeta(interfaceType, interfaceType.MakeArrayType(), implementationType, lifestyle);
        }

        public IMeta GetMeta(Type type)
        {
            IMeta meta;
            if (registerdTypes.TryGetValue(type, out meta))
            {
                return meta;
            }
            else
            {
                throw new MicroResolverException("Type is not registered, type:" + type.FullName);
            }
        }

        public void EnterEmit(Type type)
        {
            if (circularReferenceChecker.Any(x => x == type))
            {
                circularReferenceChecker.Push(type);
                throw new MicroResolverException("Found circular reference: " + string.Join(" -> ", circularReferenceChecker.Select(x => x.Name)));
            }
            else
            {
                circularReferenceChecker.Push(type);
            }
        }

        public void ExitEmit()
        {
            circularReferenceChecker.Pop();
        }

        public IMeta[] Compile()
        {
            var index = 0;
            var list = new IMeta[registerdTypes.Count];

            foreach (var item in registerdTypes)
            {
                var t = item.Key;

                var createMethod = new DynamicMethod("Create", t, Type.EmptyTypes, item.Value.OwnerType.GetTypeInfo().Module, true);
                var il = createMethod.GetILGenerator();
                item.Value.EmitNewInstance(this, il, forceEmit: true);
                il.Emit(OpCodes.Ret);

                var factory = createMethod.CreateDelegate(typeof(Func<>).MakeGenericType(t));

                if (item.Value.Lifestyle == Lifestyle.Singleton)
                {
                    var lazyFactory = Activator.CreateInstance(typeof(Lazy<>).MakeGenericType(t), new object[] { factory });
                    var lazyValue = Activator.CreateInstance(typeof(SingletonValue<>).MakeGenericType(t), lazyFactory);
                    factory = (Delegate)typeof(SingletonValue<>).MakeGenericType(t).GetRuntimeField("func").GetValue(lazyValue);
                }

                var setFactory = typeof(ObjectResolver).GetRuntimeMethods().First(x => x.Name == "SetFactory").MakeGenericMethod(item.Key);
                setFactory.Invoke(Resolver, new object[] { factory });

                item.Value.EmittedDelegate = factory;
                list[index++] = item.Value;
            }

            return list;
        }

#if NET_45

        internal AssemblyBuilder DebuggingCompile(string moduleName)
        {
            var assembly = new DynamicAssembly(moduleName);

            var type = assembly.ModuleBuilder.DefineType("DynamicFunc", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract, null);

            foreach (var item in registerdTypes)
            {
                var t = item.Key;
                var createMethod = type.DefineMethod(item.Key.FullName.Replace(".", "_"), MethodAttributes.Static | MethodAttributes.Public, t, Type.EmptyTypes);
                var il = createMethod.GetILGenerator();
                item.Value.EmitNewInstance(this, il, forceEmit: true);
                il.Emit(OpCodes.Ret);
            }

            type.CreateTypeInfo().AsType();
            return assembly.Save();
        }

#endif
    }

    internal class SingletonValue<T>
        where T : class
    {
        public Func<T> func; // Cache<T>.factory for singleton

        Lazy<T> lazyFactory;
        T value;

        public SingletonValue(Lazy<T> lazyFactory)
        {
            this.lazyFactory = lazyFactory;
            this.func = new Func<T>(GetValue);
        }

        public T GetValue()
        {
            return value ?? (value = lazyFactory.Value);
        }
    }
}