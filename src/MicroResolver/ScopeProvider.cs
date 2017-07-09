using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace MicroResolver
{
    public abstract class ScopeProvider
    {
        public static ScopeProvider Standard => new StandardScopeProvider();
        public static ScopeProvider ThreadLocal => new ThreadLocalScopeProvider();
        public static ScopeProvider AsyncLocal => new AsyncLocalScopeProvider();

        public IScopedObjectResolver Create(IObjectResolver resolver)
        {
            return new ScopedResolver(this, resolver);
        }

        public abstract void Initialize(IObjectResolver resolver);
        protected abstract object GetValueFromScoped(Type type, out bool isFirstCreated);

        class ScopedResolver : IScopedObjectResolver, IDisposable
        {
            bool isDisposed;
            readonly ScopeProvider provider;
            readonly IObjectResolver resolver;
            HashSet<IDisposable> disposeTarget;

            public ScopedResolver(ScopeProvider provider, IObjectResolver resolver)
            {
                this.provider = provider;
                this.resolver = resolver;
                this.disposeTarget = new HashSet<IDisposable>();
            }

            public IScopedObjectResolver BeginScope(ScopeProvider provider)
            {
                if (isDisposed) throw new ObjectDisposedException(this.GetType().Name);

                return provider.Create(this);
            }

            public Lifestyle Lifestyle(Type type)
            {
                return resolver.Lifestyle(type);
            }

            public T Resolve<T>()
            {
                if (isDisposed) throw new ObjectDisposedException(this.GetType().Name);

                var lifestyle = resolver.Lifestyle(typeof(T));

                switch (lifestyle)
                {
                    case MicroResolver.Lifestyle.Transient:
                    case MicroResolver.Lifestyle.Singleton:
                        return resolver.Resolve<T>();
                    case MicroResolver.Lifestyle.Scoped:
                        bool isFirstCreated;
                        var v = (T)provider.GetValueFromScoped(typeof(T), out isFirstCreated);
                        if (isFirstCreated && v is IDisposable)
                        {
                            lock (disposeTarget)
                            {
                                disposeTarget.Add((IDisposable)v);
                            }

                        }
                        return v;
                    default:
                        throw new InvalidOperationException("Invalid Lifestyle:" + lifestyle);
                }
            }

            public object Resolve(Type type)
            {
                if (isDisposed) throw new ObjectDisposedException(this.GetType().Name);

                var lifestyle = resolver.Lifestyle(type);

                switch (lifestyle)
                {
                    case MicroResolver.Lifestyle.Transient:
                    case MicroResolver.Lifestyle.Singleton:
                        return resolver.Resolve(type);
                    case MicroResolver.Lifestyle.Scoped:
                        bool isFirstCreated;
                        var v = provider.GetValueFromScoped(type, out isFirstCreated);
                        if (isFirstCreated && v is IDisposable)
                        {
                            lock (disposeTarget)
                            {
                                disposeTarget.Add((IDisposable)v);
                            }
                        }
                        return v;

                    default:
                        throw new InvalidOperationException("Invalid Lifestyle:" + lifestyle);
                }
            }

            public void Dispose()
            {
                isDisposed = true;
                foreach (var item in disposeTarget)
                {
                    item.Dispose();
                }
                disposeTarget = null;
            }
        }

        // default providers

        class StandardScopeProvider : ScopeProvider
        {
            IObjectResolver resolver;
            ConcurrentDictionary<Type, Lazy<object>> objects = new ConcurrentDictionary<Type, Lazy<object>>();
            Func<Type, Lazy<object>> createValue;

            public override void Initialize(IObjectResolver resolver)
            {
                this.resolver = resolver;
                this.createValue = CreateValue;
            }

            protected override object GetValueFromScoped(Type type, out bool isFirstCreated)
            {
                var v = objects.GetOrAdd(type, createValue);
                isFirstCreated = !v.IsValueCreated; // allow if thread unsafe(check by HashSet<> of caller)
                return v.Value;
            }

            Lazy<object> CreateValue(Type key)
            {
                return new Lazy<object>(() => resolver.Resolve(key));
            }
        }

        class ThreadLocalScopeProvider : ScopeProvider
        {
            IObjectResolver resolver;
            ConcurrentDictionary<Type, ThreadLocal<object>> objects = new ConcurrentDictionary<Type, ThreadLocal<object>>();
            Func<Type, ThreadLocal<object>> createValue;

            public override void Initialize(IObjectResolver resolver)
            {
                this.resolver = resolver;
                this.createValue = CreateValue;
            }

            protected override object GetValueFromScoped(Type type, out bool isFirstCreated)
            {
                var v = objects.GetOrAdd(type, createValue);
                isFirstCreated = !v.IsValueCreated; // allow if thread unsafe(check by HashSet<> of caller)
                return v.Value;
            }

            ThreadLocal<object> CreateValue(Type key)
            {
                return new ThreadLocal<object>(() => resolver.Resolve(key));
            }
        }

        class AsyncLocalScopeProvider : ScopeProvider
        {
            IObjectResolver resolver;
            ConcurrentDictionary<Type, AsyncLocal<object>> objects = new ConcurrentDictionary<Type, AsyncLocal<object>>();
            Func<Type, AsyncLocal<object>> createValue;

            public override void Initialize(IObjectResolver resolver)
            {
                this.resolver = resolver;
                this.createValue = CreateValue;
            }

            protected override object GetValueFromScoped(Type type, out bool isFirstCreated)
            {
                var v = objects.GetOrAdd(type, createValue);
                var value = v.Value;
                if (value == null)
                {
                    v.Value = value = resolver.Resolve(type);
                    isFirstCreated = true;
                }
                else
                {
                    isFirstCreated = false;
                }

                return value;
            }

            AsyncLocal<object> CreateValue(Type key)
            {
                return new AsyncLocal<object>();
            }
        }
    }
}