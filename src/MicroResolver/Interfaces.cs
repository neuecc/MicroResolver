using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MicroResolver
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class InjectAttribute : Attribute
    {
    }

    public enum Lifestyle
    {
        Transient = 1,
        Scoped = 2,
        Singleton = 3
    }

    public enum LifestyleType
    {
        NotRegistered = 0,
        Transient = 1,
        Scoped = 2,
        Singleton = 3
    }

    public abstract class ScopeProvider
    {
        public abstract IObjectResolver Create(IObjectResolver resolver);
    }

    public interface IObjectResolver
    {
        IObjectResolver BeginScope(ScopeProvider provider);
        LifestyleType LifestyleType<T>();
        T Resolve<T>();
    }

    //public class ScopedResolver : IObjectResolver
    //{
    //    readonly IObjectResolver resolver;

    //    public ScopedResolver(IObjectResolver resolver)
    //    {
    //        this.resolver = resolver;
    //    }

    //    public IObjectResolver BeginScope(ScopeProvider provider)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public LifestyleType LifestyleType<T>()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public T Resolve<T>()
    //    {
    //        //throw new NotImplementedException();

    //        var type = resolver.LifestyleType<T>();
    //        switch (type)
    //        {
    //            case MicroResolver.LifestyleType.NotRegistered:
    //                break;
    //            case MicroResolver.LifestyleType.Transient:
    //            case MicroResolver.LifestyleType.Singleton:
    //                break;
    //            case MicroResolver.LifestyleType.Scoped:
    //                // get from scoped cache?

    //                break;
    //                break;
    //            default:
    //                break;
    //        }
    //    }
    //}


    //public interface IObjectRegister
    //{
    //    void Register<T>(Lifestyle lifestyle, Func<T> factory);
    //    void RegisterMany<T>(Lifestyle lifestyle, params Func<T>[] factory);

    //    // others, extension methods.

    //    //void Register<T>(Func<T> factory);
    //    //void Register<T>(Func<Task<T>> asyncFactory);

    //    //void Register<T>(Lifestyle lifestyle, Func<Task<T>> asyncFactory);
    //    //   void Register<TInterface, TImplementation>();
    //    //void Register<TInterface, TImplementation>(Lifestyle lifestyle);

    //    //void RegisterMany<T>(params Func<T>[] factory);
    //    //void RegisterMany<T>(params Func<Task<T>>[] asyncFactory);

    //    //void RegisterMany<T>(Lifestyle lifestyle, params Func<Task<T>>[] asyncFactory);
    //    //void RegisterMany<TInterface>(params Type[] implementationTypes);
    //    //void RegisterMany<TInterface>(Lifestyle lifestyle, params Type[] implementationTypes);
    //}
}