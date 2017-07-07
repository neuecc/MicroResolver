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