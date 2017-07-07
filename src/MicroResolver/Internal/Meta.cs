using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MicroResolver.Internal
{
    internal interface IMeta
    {
        void EmitNewInstance(CompilationContext context, ILGenerator il, bool forceEmit = false);
    }

    internal class Meta : IMeta
    {
        public Type InterfaceType { get; }
        public Type Type { get; }
        public TypeInfo TypeInfo { get; }
        public Lifestyle Lifestyle { get; }
        public ConstructorInfo Constructor { get; }
        public FieldInfo[] InjectFields { get; }
        public PropertyInfo[] InjectProperties { get; }
        public MethodInfo[] InjectMethods { get; }

        public Meta(Type interfaceType, Type type, Lifestyle lifestyle)
        {
            this.InterfaceType = interfaceType;
            this.Type = type;
            this.TypeInfo = type.GetTypeInfo();
            this.Lifestyle = lifestyle;

            if (TypeInfo.IsValueType)
            {
                throw new Exception("Does not support ValueType, type:" + type.Name);
            }

            // Constructor, single [Inject] constructor -> single most parameters constuctor 
            var injectConstructors = this.TypeInfo.DeclaredConstructors.Where(x => x.GetCustomAttribute<InjectAttribute>(true) != null).ToArray();
            if (injectConstructors.Length == 0)
            {
                var grouped = this.TypeInfo.DeclaredConstructors.GroupBy(x => x.GetParameters().Length).OrderByDescending(x => x.Key).FirstOrDefault();
                if (grouped == null)
                {
                    throw new Exception("Type does not found injectable constructor, type:" + type.Name);
                }
                else if (grouped.Count() != 1)
                {
                    throw new Exception("Type found multiple ambiguous constructors, type:" + type.Name);
                }
                else
                {
                    this.Constructor = grouped.First();
                }
            }
            else if (injectConstructors.Length == 1)
            {
                this.Constructor = injectConstructors[0];
            }
            else
            {
                throw new Exception("Type found multiple [Inject] marked constructors, type:" + type.Name);
            }

            // Fields, [Inject] Only
            this.InjectFields = this.Type.GetRuntimeFields().Where(x => x.GetCustomAttribute<InjectAttribute>(true) != null).ToArray();

            // Properties, [Inject] only
            this.InjectProperties = this.Type.GetRuntimeProperties().Where(x => x.SetMethod != null && x.GetCustomAttribute<InjectAttribute>(true) != null).ToArray();

            // Methods, [Inject] Only
            this.InjectMethods = this.Type.GetRuntimeMethods().Where(x => x.GetCustomAttribute<InjectAttribute>(true) != null).ToArray();
        }

        public void EmitNewInstance(CompilationContext context, ILGenerator il, bool forceEmit = false)
        {
            // Emit constructor parameters dependency
            // Emit create instance
            // Emit field dependency and set
            // Emit property dependency and set
            // Emit method parameters dependency
            // Emit call inject methods

            if (!forceEmit && Lifestyle == Lifestyle.Singleton)
            {
                var field = context.Resolver.cacheType.MakeGenericType(InterfaceType).GetRuntimeField("factory");
                var invoke = typeof(Func<>).MakeGenericType(InterfaceType).GetRuntimeMethod("Invoke", Type.EmptyTypes);
                il.Emit(OpCodes.Ldsfld, field);
                il.Emit(OpCodes.Call, invoke);
                return;
            }

            context.EnterEmit(Type); // check circular reference

            // constructor
            foreach (var item in Constructor.GetParameters())
            {
                var meta = context.GetMeta(item.ParameterType);
                meta.EmitNewInstance(context, il);
            }
            il.Emit(OpCodes.Newobj, Constructor);

            // field
            foreach (var item in InjectFields)
            {
                il.Emit(OpCodes.Dup);
                var meta = context.GetMeta(item.FieldType);
                meta.EmitNewInstance(context, il);
                il.Emit(OpCodes.Stfld, item);
            }

            // property
            foreach (var item in InjectProperties)
            {
                il.Emit(OpCodes.Dup);
                var meta = context.GetMeta(item.PropertyType);
                meta.EmitNewInstance(context, il);
                EmitCall(il, item.SetMethod);
            }

            // methods
            foreach (var item in InjectMethods)
            {
                il.Emit(OpCodes.Dup);
                foreach (var p in item.GetParameters())
                {
                    var meta = context.GetMeta(p.ParameterType);
                    meta.EmitNewInstance(context, il);
                }
                EmitCall(il, item);
            }

            context.ExitEmit();
        }

        // call-callvirt optimization
        static void EmitCall(ILGenerator il, MethodInfo methodInfo)
        {
            if (methodInfo.IsFinal || !methodInfo.IsVirtual)
            {
                il.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, methodInfo);
            }
        }
    }

    internal class CollectionMeta : IMeta
    {
        public Type InterfaceType { get; }
        public Meta[] Types { get; }
        public Lifestyle Lifestyle { get; }

        public CollectionMeta(Type interfaceType, Meta[] types, Lifestyle lifestyle)
        {
            this.InterfaceType = interfaceType;
            this.Types = types;
            this.Lifestyle = lifestyle;
        }

        public void EmitNewInstance(CompilationContext context, ILGenerator il, bool forceEmit = false)
        {
            if (!forceEmit && Lifestyle == Lifestyle.Singleton)
            {
                var field = context.Resolver.cacheType.MakeGenericType(InterfaceType).GetRuntimeField("factory");
                var invoke = typeof(Func<>).MakeGenericType(InterfaceType).GetRuntimeMethod("Invoke", Type.EmptyTypes);
                il.Emit(OpCodes.Ldsfld, field);
                il.Emit(OpCodes.Call, invoke);
                return;
            }

            EmitLdc_I4(il, Types.Length);
            il.Emit(OpCodes.Newarr);

            for (int i = 0; i < Types.Length; i++)
            {
                il.Emit(OpCodes.Dup);
                EmitLdc_I4(il, i);
                var meta = context.GetMeta(Types[i].Type);
                meta.EmitNewInstance(context, il);
                il.Emit(OpCodes.Stelem_Ref);
            }
        }

        // Ldc_I4 optimization
        static void EmitLdc_I4(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    break;
                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (value >= -128 && value <= 127)
                    {
                        il.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldc_I4, value);
                    }
                    break;
            }
        }
    }
}