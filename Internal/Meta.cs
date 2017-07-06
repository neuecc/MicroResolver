using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Collections.Concurrent;

namespace MicroResolver.Internal
{
    internal class Meta
    {
        public ConstructorInfo Constructor { get; set; }
        public PropertyInfo[] InjectProperties { get; set; }
        public FieldInfo[] InjectFields { get; set; }
        public MethodInfo[] InjectMethods { get; set; }

        static ConcurrentDictionary<Type, object> delegateCacheCheck = new ConcurrentDictionary<Type, object>();

        internal static Func<IObjectResolver, T> CreateFactory<T>()
        {
            var t = typeof(T);
            var createMethod = new DynamicMethod("Create", t, new[] { typeof(IObjectResolver) }, t, true);
            var il = createMethod.GetILGenerator();

            // TODO:write il

            var result = createMethod.CreateDelegate(typeof(Func<IObjectResolver, T>));
            return (Func<IObjectResolver, T>)result;
        }
    }
}
