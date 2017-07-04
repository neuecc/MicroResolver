MicroResolver
===
Extremely Fast Micro Service Locator.

Quick Start
---
Install from NuGet(.NET Standard 1.4)

* Install-Package [MicroResolver](https://www.nuget.org/packages/MicroResolver)

```csharp
// sample classes...

interface IOrderRepository
{
}

class SqlOrderRepository : IOrderRepository
{
}

interface ILogger
{
}

class FileLogger : ILogger
{
}

public class CancelOrderHandler
{
    readonly ILogger logger;

    public CancelOrderHandler(IObjectResolver resolver) // only supports service locator
    {
        this.logger = resolver.Resolve<ILogger>();
    }
}

class Program
{
    static Program()
    {
        // 1. Configure the container (register)
        TransientResolver.Instance.Register<IOrderRepository>(() => new SqlOrderRepository());
        SingletonResolver.Instance.Register<ILogger>(() => new FileLogger());
        TransientResolver.Instance.Register(() => new CancelOrderHandler(CompositeResolver.Instance));
    }

    static void Main(string[] args)
    {
        // 2. Use the container
        var handler = CompositeResolver.Instance.Resolve<CancelOrderHandler>();

        var orderId = Guid.Parse(args[0]);
        // ...
    }
}
```

If transient, you can use `TransientResolver`. If singleton, you can use `SingletonResolver. And you can register factory method by `Register` or `RegisterMany`.

`CompositeResolver` is best way to resolve instance, it resolve to use `SingletonResolver` at first, if not registered, use `TransientResolver`.

`IObjectResolver` can be used ServiceLocator pattern. MicroResolver does not support autowire dependency injection.

Performance and Technology
---
Performance is extremely fast because value factory is cached in generic type.

```csharp
public class TransientResolver : IResolveContainer
{
    public static readonly IResolveContainer Instance = new TransientResolver();

    // snip...

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

    public void Register<T>(Func<T> factory)
    {
        Cache<T>.factory = factory;
    }

    static class Cache<T>
    {
        public static Func<T> factory;
    }
}
```

No reflection, No type generation, No delegate caching, No overhead. This is the fastest way to get instance.