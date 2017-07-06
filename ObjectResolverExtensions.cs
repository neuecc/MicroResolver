using System;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using MicroResolver.Internal;

namespace MicroResolver
{
    public class MicroResolverException : Exception
    {
        public MicroResolverException(string message)
            : base(message)
        {

        }
    }

    public static class ObjectResolverExtensions
    {
        public static T Resolve<T>(this IObjectResolver resolver)
        {
            T instance;
            if (resolver.TryResolve<T>(out instance))
            {
                return instance;
            }
            else
            {
                throw new Exception("Can't resolve type:" + typeof(T).Name);
            }
        }

        public static IEnumerable<T> ResolveMany<T>(this IObjectResolver resolver) where T : class
        {
            IEnumerable<T> instance;
            if (resolver.TryResolveMany<T>(out instance))
            {
                return instance;
            }
            else
            {
                throw new Exception("Can't resolve type:" + typeof(T).Name);
            }
        }

        public static void Register<T>(this ObjectResolver resolver)
        {
            var factory = Meta.CreateFactory<T>();
            resolver.Register<T>(Lifestyle.Transient, () => factory(resolver));
        }

        public static void Register<TInterface, TImplementation>(this ObjectResolver resolver)
            where TImplementation : TInterface
        {
            var factory = Meta.CreateFactory<TImplementation>();
            resolver.Register<TInterface>(Lifestyle.Transient, () => factory(resolver));
        }


        // NonGeneric

        static readonly ConcurrentDictionary<Type, Func<IObjectResolver, object>> callResolve = new ConcurrentDictionary<Type, Func<IObjectResolver, object>>();
        static readonly ConcurrentDictionary<Type, Func<IObjectResolver, IEnumerable<object>>> callResolveMany = new ConcurrentDictionary<Type, Func<IObjectResolver, IEnumerable<object>>>();

        public static object Resolve(this IObjectResolver resolver, Type type)
        {
            Func<IObjectResolver, object> func;
            if (!callResolve.TryGetValue(type, out func))
            {
                var param1 = Expression.Parameter(typeof(IObjectResolver), "resolver");
                var methodInfo = typeof(ObjectResolverExtensions).GetRuntimeMethod("Resolve", new[] { typeof(IObjectResolver) }).MakeGenericMethod(type);
                var call = Expression.Call(methodInfo, param1);
                var body = Expression.Convert(call, typeof(object));

                func = Expression.Lambda<Func<IObjectResolver, object>>(body, param1).Compile();

                callResolve[type] = func;
            }

            return func.Invoke(resolver);
        }

        public static IEnumerable<object> ResolveMany(this IObjectResolver resolver, Type type)
        {
            Func<IObjectResolver, IEnumerable<object>> func;
            if (!callResolveMany.TryGetValue(type, out func))
            {
                var param1 = Expression.Parameter(typeof(IObjectResolver), "resolver");
                var methodInfo = typeof(ObjectResolverExtensions).GetRuntimeMethod("ResolveMany", new[] { typeof(IObjectResolver) }).MakeGenericMethod(type);
                var call = Expression.Call(methodInfo, param1);
                var body = Expression.Convert(call, typeof(IEnumerable<object>));

                func = Expression.Lambda<Func<IObjectResolver, IEnumerable<object>>>(body, param1).Compile();

                callResolveMany[type] = func;
            }

            return func.Invoke(resolver);
        }
    }
}
