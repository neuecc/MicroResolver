using MicroResolver.Internal;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace MicroResolver
{
    public abstract partial class ObjectResolver : IObjectResolver
    {
        const string ModuleName = "MicroResolver.DynamicObjectResolver";

        internal static readonly DynamicAssembly assembly;

        internal Type cacheType;

        static ObjectResolver()
        {
            assembly = new DynamicAssembly(ModuleName);


        }

#if DEBUG && NET_45
        internal AssemblyBuilder Save()
        {
            return assembly.Save();
        }
#endif

        public static ObjectResolver Create()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "");
            var resolverType = assembly.ModuleBuilder.DefineType("ObjectResolver_" + id, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, typeof(ObjectResolver));

            var cacheType = assembly.ModuleBuilder.DefineType("Cache_" + id, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract, null);
            var genericP = cacheType.DefineGenericParameters("T")[0].AsType();

            var f = cacheType.DefineField("factory", typeof(Func<>).MakeGenericType(genericP), FieldAttributes.Static | FieldAttributes.Public);
            {
                var cctor = cacheType.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
                var il = cctor.GetILGenerator();
                il.Emit(OpCodes.Ldsfld, TypeBuilder.GetField(typeof(FuncHelper<>).MakeGenericType(genericP), typeof(FuncHelper<>).GetRuntimeField("ThrowException")));
                il.Emit(OpCodes.Stsfld, f);
                il.Emit(OpCodes.Ret);
            }

            var generatedCacheType = cacheType.CreateTypeInfo().AsType();
            {
                var m = resolverType.DefineMethod("Resolve", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual);
                var gpp = m.DefineGenericParameters("T")[0].AsType();
                m.SetReturnType(gpp);

                var f2 = TypeBuilder.GetField(generatedCacheType.MakeGenericType(gpp), generatedCacheType.GetRuntimeField(f.Name));
                var invoke = TypeBuilder.GetMethod(typeof(Func<>).MakeGenericType(gpp), typeof(Func<>).GetRuntimeMethod("Invoke", Type.EmptyTypes));

                var il = m.GetILGenerator();
                il.Emit(OpCodes.Ldsfld, f2);
                il.Emit(OpCodes.Call, invoke);
                il.Emit(OpCodes.Ret);
            }
            {
                var m = resolverType.DefineMethod("SetFactory", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual);
                var gpp = m.DefineGenericParameters("T")[0].AsType();
                m.SetParameters(typeof(Func<>).MakeGenericType(gpp));

                var f2 = TypeBuilder.GetField(generatedCacheType.MakeGenericType(gpp), generatedCacheType.GetRuntimeField(f.Name));

                var il = m.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stsfld, f2);
                il.Emit(OpCodes.Ret);
            }

            var resolver = (ObjectResolver)Activator.CreateInstance(resolverType.CreateTypeInfo().AsType());
            resolver.cacheType = generatedCacheType;

            return resolver;
        }
    }
}

namespace MicroResolver.Internal
{
    public static class FuncHelper<T>
    {
        public static Func<T> ThrowException;

        static FuncHelper()
        {
            ThrowException = new Func<T>(() =>
            {
                throw new MicroResolverException("Resolve<T> was failed, not registered. Type: " + typeof(T).FullName);
            });
        }
    }
}