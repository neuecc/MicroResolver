using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using MicroResolver.Internal;
using System.Reflection;
using System.Reflection.Emit;

namespace MicroResolver
{
    public abstract partial class ObjectResolver : IObjectResolver
    {
        const string ModuleName = "MicroResolver.DynamicObjectResolver";
        internal static readonly DynamicAssembly assembly;

        static ObjectResolver()
        {
            assembly = new DynamicAssembly(ModuleName);
        }

        public static ObjectResolver Create()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "");
            var resolverType = assembly.ModuleBuilder.DefineType("ObjectResolver_" + id, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed, typeof(ObjectResolver));

            var cacheType = assembly.ModuleBuilder.DefineType("Cache_" + id, TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract, null);
            var genericP = cacheType.DefineGenericParameters("T")[0].AsType();

            var f = cacheType.DefineField("factory", typeof(CachedFactory<>).MakeGenericType(genericP), FieldAttributes.Static | FieldAttributes.Public);
            var fm = cacheType.DefineField("factoryMany", typeof(CachedFactory<>).MakeGenericType(typeof(IEnumerable<>).MakeGenericType(genericP)), FieldAttributes.Static | FieldAttributes.Public);

            var generatedCacheType = cacheType.CreateTypeInfo().AsType();

            // impl methods
            {
                var m = resolverType.DefineMethod("GetCachedFactory", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual);
                var gpp = m.DefineGenericParameters("T")[0].AsType();
                m.SetReturnType(typeof(CachedFactory<>).MakeGenericType(gpp).MakeByRefType());

                var f2 = TypeBuilder.GetField(generatedCacheType.MakeGenericType(gpp), generatedCacheType.GetRuntimeField(f.Name));

                var il = m.GetILGenerator();
                il.Emit(OpCodes.Ldsflda, f2);
                il.Emit(OpCodes.Ret);
            }
            {
                var m = resolverType.DefineMethod("GetCachedManyFactory", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual);
                var gpp = m.DefineGenericParameters("T")[0].AsType();
                m.SetReturnType(typeof(CachedFactory<>).MakeGenericType(typeof(IEnumerable<>).MakeGenericType(gpp)).MakeByRefType());

                var f2 = TypeBuilder.GetField(generatedCacheType.MakeGenericType(gpp), generatedCacheType.GetRuntimeField(fm.Name));

                var il = m.GetILGenerator();
                il.Emit(OpCodes.Ldsflda, f2);
                il.Emit(OpCodes.Ret);
            }
            {
                var m = resolverType.DefineMethod("SetCachedFactory", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual);
                var gpp = m.DefineGenericParameters("T")[0].AsType();
                m.SetParameters(typeof(CachedFactory<>).MakeGenericType(gpp));

                var f2 = TypeBuilder.GetField(generatedCacheType.MakeGenericType(gpp), generatedCacheType.GetRuntimeField(f.Name));

                var il = m.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stsfld, f2);
                il.Emit(OpCodes.Ret);
            }
            {
                var m = resolverType.DefineMethod("SetCachedManyFactory", MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual);
                var gpp = m.DefineGenericParameters("T")[0].AsType();
                m.SetParameters(typeof(CachedFactory<>).MakeGenericType(typeof(IEnumerable<>).MakeGenericType(gpp)));

                var f2 = TypeBuilder.GetField(generatedCacheType.MakeGenericType(gpp), generatedCacheType.GetRuntimeField(fm.Name));

                var il = m.GetILGenerator();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stsfld, f2);
                il.Emit(OpCodes.Ret);
            }

            return (ObjectResolver)Activator.CreateInstance(resolverType.CreateTypeInfo().AsType());
        }
    }
}