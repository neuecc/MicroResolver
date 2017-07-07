using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace MicroResolver.Internal
{
    // TODO:internal
    public class CompilationContext
    {
        Dictionary<Type, Meta> registerdTypes = new Dictionary<Type, Meta>();
        Dictionary<Type, object> singletonCache = new Dictionary<Type, object>();
        Stack<Type> circularReferenceChecker = new Stack<Type>();

        public ObjectResolver Resolver { get; }

        public CompilationContext(ObjectResolver resolver)
        {
            this.Resolver = resolver;
        }

        public void Add(Type interfaceType, Type implementationType, Lifestyle lifestyle)
        {
            registerdTypes.Add(interfaceType, new Meta(interfaceType, implementationType, lifestyle));
        }

        public Meta GetMeta(Type type)
        {
            Meta meta;
            if (registerdTypes.TryGetValue(type, out meta))
            {
                return meta;
            }
            else
            {
                throw new Exception("Type is not registered, type:" + type.FullName);
            }
        }

        public void EnterEmit(Type type)
        {
            if (circularReferenceChecker.Any(x => x == type))
            {
                circularReferenceChecker.Push(type);
                throw new Exception("Found circular reference:" + string.Join(" -> ", circularReferenceChecker.Select(x => x.Name)));
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

        public void Compile()
        {
            foreach (var item in registerdTypes)
            {
                var t = item.Value.Type;
                var createMethod = new DynamicMethod("Create", t, Type.EmptyTypes, t, true);
                var il = createMethod.GetILGenerator();
                item.Value.EmitNewInstance(this, il, forceEmit: true);
                il.Emit(OpCodes.Ret);

                var factory = createMethod.CreateDelegate(typeof(Func<>).MakeGenericType(t));

                if (item.Value.Lifestyle == Lifestyle.Singleton)
                {
                    var lazyFactory = Activator.CreateInstance(typeof(Lazy<>).MakeGenericType(t), new object[] { factory });
                    var lazyValue = Activator.CreateInstance(typeof(LazyValue<>).MakeGenericType(t), lazyFactory);
                    factory = (Delegate)typeof(LazyValue<>).MakeGenericType(t).GetRuntimeField("func").GetValue(lazyValue);
                }

                var setFactory = typeof(ObjectResolver).GetRuntimeMethods().First(x => x.Name == "SetFactory").MakeGenericMethod(item.Key);
                setFactory.Invoke(Resolver, new object[] { factory });
            }
        }

#if NET_45

        public AssemblyBuilder DebuggingCompile(string moduleName)
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

    internal class LazyValue<T>
    {
        public Func<T> func;
        Lazy<T> lazyFactory;
        T value;
        bool isValueCreated;

        public LazyValue(Lazy<T> lazyFactory)
        {
            this.lazyFactory = lazyFactory;
            this.func = new Func<T>(GetValue);
        }

        public T GetValue()
        {
            if (!isValueCreated)
            {
                var v = lazyFactory.Value;
                value = v;
                isValueCreated = true;
                return v;
            }
            else
            {
                return value;
            }
        }
    }
}