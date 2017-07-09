MicroResolver
===
Extremely Fast Dependency Injection Library.

Features
---
MicroResolver is desgined for peformance. I've released two fastest serializers [ZeroFormatter](https://github.com/neuecc/ZeroFormatter) and [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp), this library is using there dynamic il code generation technique.

* [Dynamic IL Inlining](https://github.com/neuecc/MicroResolver#perforamnce-technique---dynamic-il-inlining)
* [Generic Type Caching per resolver](https://github.com/neuecc/MicroResolver#perforamnce-technique---generic-type-caching-per-resolver)
* [Fast NonGeneric lookup table](https://github.com/neuecc/MicroResolver#perforamnce-technique---fast-nongeneric-lookup-table)

Marked fastest on Transient, Combined and IEnumerable by [IoCPerformance](https://github.com/danielpalme/IocPerformance).

|**Container**|**Singleton**|**Transient**|**Combined**|**Complex**|
|:------------|------------:|------------:|-----------:|----------:|
|**No**|53<br/>50|58<br/>51|71<br/>73|87<br/>67|
|**[abioc 0.6.0](https://github.com/JSkimming/abioc)**|46<br/>47|67<br/>55|72<br/>66|86<br/>65|
|**[Autofac 4.6.0](https://github.com/autofac/Autofac)**|562<br/>477|545<br/>488|1408<br/>1252|4726<br/>4350|
|**[DryIoc 2.10.4](https://bitbucket.org/dadhi/dryioc)**|49<br/>37|47<br/>47|62<br/>60|**69**<br/>57|
|**[fFastInjector 1.0.1](https://ffastinjector.codeplex.com)**|**21**<br/>**27**|61<br/>52|145<br/>108|373<br/>223|
|**[Mef2 1.0.30.0](https://blogs.msdn.com/b/bclteam/p/composition.aspx)**|187<br/>119|199<br/>133|274<br/>159|447<br/>266|
|**[MicroResolver 2.0.0](https://github.com/neuecc/MicroResolver)**|26<br/>33|**31**<br/>**39**|**50**<br/>**55**|72<br/>63|
|**[Ninject 3.2.2.0](http://ninject.org)**|3978<br/>2444|12567<br/>7963|34620<br/>19315|95859*<br/>60936*|
|**[SimpleInjector 4.0.8](https://simpleinjector.org)**|58<br/>44|82<br/>59|93<br/>76|109<br/>80|
|**[Unity 4.0.1](http://msdn.microsoft.com/unity)**|1992<br/>1042|2745<br/>1523|7161<br/>3843|19892<br/>10586|

Full Result is [here](https://gist.github.com/neuecc/edc9daa035f3878d0eb0a31d9f76ed24).

Support Features - Consturctor Injection, Field Injection, Property Injection, Method Injection, Collection resolver and Three lifetime support(Singleton, Transient and Scoped).

Quick Start
---
Install from NuGet(for .NET Framework 4.6, .NET Standard 1.4)

* Install-Package [MicroResolver](https://www.nuget.org/packages/MicroResolver)

```csharp
// Create a new container
var resolver = ObjectResolver.Create();

// Register interface->type map, default is transient(instantiate every request)
resolver.Register<IUserRepository, SqlUserRepository>();

// You can configure lifestyle - Transient, Singleton or Scoped
resolver.Register<ILogger, MailLogger>(Lifestyle.Singleton);

// Compile and Verify container(this is required step)
resolver.Compile();

// Get instance from container
var userRepository = resolver.Resolve<IUserRepository>();
var logger = resolver.Resolve<ILogger>();
```

Notice: MicroResolver requests call `Compile` before use container.

InjectionAttribute and Resolve Collection
---
MicroResolver can resolve all public and private properties, fields, constructor and methods. Inject target have to mark `[Inject]` attribute.

```csharp
public class MyType : IMyType
{
    // field injection

    [Inject]
    public IInjectTarget PublicField;

    [Inject]
    IInjectTarget PrivateField;

    // property injection

    [Inject]
    public IInjectTarget PublicProperty { get; set; }

    [Inject]
    IInjectTarget PrivateProperty { get; set; }

    // constructor injection
    // if not marked [Inject], the constructor with the most parameters is used.
    [Inject]
    public MyType(IInjectTarget x, IInjectTarget y, IInjectTarget z)
    {

    }

    // method injection

    [Inject]
    public void Initialize1()
    {
    }

    [Inject]
    public void Initialize2()
    {
    }
}

// and resolve it
var v = resolver.Resolve<IMyType>();
```

Inject order is `Constructor -> Field -> Property -> Method`.

If register many types per type, you can use `RegisterCollection` and `Resolve<IEnumerable<T>>`.

```csharp
// Register type -> many types
resolver.RegisterCollection<IMyType>(typeof(T1), typeof(T2), typeof(T3));

resolver.Compile();

// can resolve by IEnumerbale<T> or T[] or IReadOnlyList<T>.
resolver.Resolve<IEnumerable<IMyType>>();
resolver.Resolve<IMyType[]>();
resolver.Resolve<IReadOnlyList<IMyType>>();

// can resolve other type's inject target.
public class AnotherType
{
    public AnotherType(IMyType[] targets)
    {
    }
}
```

Scoped
---
`Lifetime.Scoped` is usually the same as Transient but within BeginScope it behaves like a singleton in the scope.

```csharp
// sample type of check scope
public class MyClass : IMyType, IDisposable
{
    public MyClass()
    {
        Console.WriteLine("Created");
    }

    public void Dispose()
    {
        Console.WriteLine("Disposed");
    }
}

// -----------

var resolver = ObjectResolver.Create();
resolver.Register<IMyType, MyClass>(Lifestyle.Scoped);
resolver.Compile();

using (var coResolver = resolver.BeginScope(ScopeProvider.Standard))
{
    var i1 = coResolver.Resolve<IMyType>(); // "Created"
    var i2 = coResolver.Resolve<IMyType>();

    Console.WriteLine(Object.ReferenceEquals(i1, i2)); // "True" -> same instance

    // if scope end and instantiated types is IDisposable, called Dispose.
} // "Disposed"
```

`ScopeProvider` has three option in default. ` ScopeProvider.Standard`, `ScopeProvider.ThreadLocal` and `ScopeProvider.AsyncLocal`. If needs custom scope, you can create own ScopeProvider.

```csharp
public class MyScopeProvider : ScopeProvider
{
    public override void Initialize(IObjectResolver resolver)
    {
        // when called from BeginScope().
    }

    protected override object GetValueFromScoped(Type type, out bool isFirstCreated)
    {
        // called per Resolve<T>.
    }
}
```

Perforamnce Technique - Dynamic IL Inlining
---
Everyone creates dynamic code generation for optimize performance. But if target is complex type?

```csharp
// sample of complex dependency type
public class ForPropertyInjection : IForPropertyInjection
{
    [Inject]
    public void OnCreate()
    {
    }
}

public class ForConstructorInjection : IForConsturctorInjection
{
    [Inject]
    public IForFieldInjection MyField;
}

public class ComplexType : IComplexType
{
    [Inject]
    public IForPropertyInjection MyProperty { get; set; }

    public ComplexType(IForConsturctorInjection instance1)
    {

    }

    [Inject]
    public void Initialize()
    {
    }
}

// for example, how to resolve ComplexType?
var v = resolver.Resolve<IComplexType>();
```

The following way is not slow, but it is not fastest.

```csharp
// This is `slow` example of complex type resolve
static IComplexType ResolveComplexType(IObjectResolver resolver)
{
    var a = resolver.Resolve<IForConsturctorInjection>();
    var b = resolver.Resolve<IForPropertyInjection>();

    var result = new ComplexType(a);
    result.MyProperty = b;
    result.Initialize();

    return result;
}
```

MicroResolve choose inlining code generation, all dependencies are analyzed and inlined at compile time.

```csharp
// This is actual code generation of MicroResolver, all dependency is inlined at il code generation
static ComplexType ResolveComposite()
{
    var a = new ForConstructorInjection();
    a.MyField = new ForFieldInjection();
    var b = new ForPropertyInjection();
    b.OnCreate();

    var result = new ComplexType(a);
    result.MyProperty = b;
    result.Initialize();

    return result;
}
```

Perforamnce Technique - Generic Type Caching per resolver
---
The generated code is cached. And how to retrieve it? ConcurrentDictionary? Dictionary? They are slow. MicroResolve choose generic type caching.

This is `ObjectResolver` signature.

```csharp
public abstract class ObjectResolver
{
    public abstract T Resolve<T>();
}
```

If called `ObjectResolver.Create`, generate dynamic inherited type.

```csharp
public class ObjectResolver_Generated : ObjectResolver
{
    public override T Resolve<T>()
    {
        // too simple, of course simple is fastest.
        return Cache<T>.factory();
    }

    Cache<T>
    {
        // generated Func<T> code is see 'Dynamic IL Inlining' section.
        public Func<T> factory;
    }
}
```

The code path is too short, it means no overhead.

> But generated container can not remove. This is a design constraint.

Perforamnce Technique - Fast NonGeneric lookup table
---
Type Caching is require to use generics method. But often framework requests nongeneric type.

```csharp
// fastest
resolver.Resolve<T>();

// slower but framework requests this method
(T)resolver.Resolve(type);
```

MicroResolver use fast type lookup by own fixed hashtable.

```csharp
// buckets item
struct HashTuple
{
    public Type type;
    public Func<object> factory;
}

// simplest hash table(fixed-array chaining hashtable)
private HashTuple[][] table;

// register - Func<T> -> Func<object> by delegate covariance
table[hash][index] = new Func<object>(Resolve<T>);

// simplest == fastest lookup
public object Get(Type type)
{
    var hashCode = type.GetHashCode();
    var buckets = table[hashCode % table.Length];

    // .Length for loop can remove array bnounds check
    for (int i = 0; i < buckets.Length; i++)
    {
        if (buckets[i].type == type)
        {
            return buckets[i].factory();
        }
    }

    throw new MicroResolverException("Type was not dound, Type: " + type.FullName);
}
```

non-generic lookup is slower than generic but still fast.

Author Info
---
Yoshifumi Kawai(a.k.a. neuecc) is a software developer in Japan.  
He is the Director/CTO at Grani, Inc.  
Grani is a mobile game developer company in Japan and well known for using C#.  
He is awarding Microsoft MVP for Visual C# since 2011.  
He is known as the creator of [UniRx](http://github.com/neuecc/UniRx/)(Reactive Extensions for Unity)  

Blog: [https://medium.com/@neuecc](https://medium.com/@neuecc) (English)  
Blog: [http://neue.cc/](http://neue.cc/) (Japanese)  
Twitter: [https://twitter.com/neuecc](https://twitter.com/neuecc) (Japanese)   

License
---
This library is under the MIT License.