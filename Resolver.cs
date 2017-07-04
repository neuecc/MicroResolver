using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// Extremely fast micro serice locator.

namespace MicroResolver
{
    public interface IObjectResolver
    {
        bool TryResolve<T>(out T instance);
        bool TryResolveMany<T>(out IEnumerable<T> instances) where T : class;
    }

    public static class ObjectResolverExtensions
    {
        static readonly ConcurrentDictionary<Type, Func<IObjectResolver, object>> callResolve = new ConcurrentDictionary<Type, Func<IObjectResolver, object>>();
        static readonly ConcurrentDictionary<Type, Func<IObjectResolver, IEnumerable<object>>> callResolveMany = new ConcurrentDictionary<Type, Func<IObjectResolver, IEnumerable<object>>>();

        public static T Resolve<T>(this IObjectResolver resolver)
        {
            T instance;
            if (resolver.TryResolve<T>(out instance))
            {
                return instance;
            }
            else
            {
                throw new MicroResolverException("Can't resolve type:" + typeof(T).Name);
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
                throw new MicroResolverException("Can't resolve type:" + typeof(T).Name);
            }
        }

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

    public class MicroResolverException : Exception
    {
        public MicroResolverException(string message)
            : base(message)
        {

        }
    }

    public interface IObjectRegister
    {
        void Register<T>(Func<T> factory);
        void RegisterMany<T>(params Func<T>[] factories) where T : class;
    }

    public interface IResolveContainer : IObjectResolver, IObjectRegister
    {
    }

    // Singleton -> Transient
    public class CompositeResolver : IObjectResolver
    {
        public static readonly IObjectResolver Instance = new CompositeResolver();

        CompositeResolver()
        {

        }

        public bool TryResolve<T>(out T instance)
        {
            if (SingletonResolver.Instance.TryResolve<T>(out instance))
            {
                return true;
            }
            else if (TransientResolver.Instance.TryResolve<T>(out instance))
            {
                return true;
            }
            return false;
        }

        public bool TryResolveMany<T>(out IEnumerable<T> instance) where T : class
        {
            if (SingletonResolver.Instance.TryResolveMany<T>(out instance))
            {
                return true;
            }
            else if (TransientResolver.Instance.TryResolveMany<T>(out instance))
            {
                return true;
            }
            return false;
        }
    }

    public class TransientResolver : IResolveContainer
    {
        public static readonly IResolveContainer Instance = new TransientResolver();

        TransientResolver()
        {

        }

        public bool TryResolve<T>(out T instance)
        {
            var f = Cache<T>.factory;
            if (f != null)
            {
                instance = f.Invoke();
                return true;
            }

            instance = default(T);
            return false;
        }

        public bool TryResolveMany<T>(out IEnumerable<T> instances) where T : class
        {
            var f = CacheMany<T>.factories;
            if (f != null)
            {
                instances = f;
                return true;
            }

            throw new MicroResolverException("Can't resolve type:" + typeof(T).Name);
        }

        public void Register<T>(Func<T> factory)
        {
            Cache<T>.factory = factory;
        }

        public void RegisterMany<T>(params Func<T>[] factories) where T : class
        {
            CacheMany<T>.factories = factories.Select(x => x());
        }

        static class Cache<T>
        {
            public static Func<T> factory;
        }

        static class CacheMany<T>
        {
            public static IEnumerable<T> factories;
        }
    }

    public class SingletonResolver : IResolveContainer
    {
        public static readonly IResolveContainer Instance = new SingletonResolver();

        SingletonResolver()
        {

        }

        public bool TryResolve<T>(out T instance)
        {
            var v = Cache<T>.value;
            instance = v.Value;
            return v.Key;
        }

        public bool TryResolveMany<T>(out IEnumerable<T> instances) where T : class
        {
            var v = CacheMany<T>.values;
            instances = v.Value;
            return v.Key;
        }

        public void Register<T>(Func<T> factory)
        {
            Cache<T>.value = new KeyValuePair<bool, T>(true, factory());
        }

        public void RegisterMany<T>(params Func<T>[] factories) where T : class
        {
            CacheMany<T>.values = new KeyValuePair<bool, T[]>(true, factories.Select(x => x()).ToArray());
        }

        static class Cache<T>
        {
            public static KeyValuePair<bool, T> value;
        }

        static class CacheMany<T>
        {
            public static KeyValuePair<bool, T[]> values;
        }
    }
}