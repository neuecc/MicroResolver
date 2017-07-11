using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MicroResolver.Test
{
    public class ResolverTest
    {
        [Fact]
        public void StandardTest()
        {
            var resolver = ObjectResolver.Create();
            resolver.Register<ISingleton1, Singleton1>(Lifestyle.Singleton);
            resolver.Register<ITransient1, Transient1>(Lifestyle.Transient);
            resolver.Register<ICombined1, Combined1>(Lifestyle.Transient);
            resolver.Register<IFirstService, FirstService>(Lifestyle.Singleton);
            resolver.Register<ISecondService, SecondService>(Lifestyle.Singleton);
            resolver.Register<IThirdService, ThirdService>(Lifestyle.Singleton);
            resolver.Register<ISubObjectOne, SubObjectOne>(Lifestyle.Transient);
            resolver.Register<ISubObjectTwo, SubObjectTwo>(Lifestyle.Transient);
            resolver.Register<ISubObjectThree, SubObjectThree>(Lifestyle.Transient);
            resolver.Register<IComplex1, Complex1>(Lifestyle.Transient);
            resolver.Compile();

            var v1 = resolver.Resolve<ISingleton1>() as Singleton1;

            v1.PublicField.IsNotNull();
            v1.PublicProperty.IsNotNull();
            v1.GetPrivateField().IsNotNull();
            v1.GetPrivateProperty().IsNotNull();
            v1.fromConstructor1.IsNotNull();
            v1.fromConstructor2.IsNotNull();
            v1.VerifyOkay();

            var v2 = resolver.Resolve<ISingleton1>();
            Object.ReferenceEquals(v1, v2).IsTrue();

            var f1 = resolver.Resolve<IFirstService>();
            Object.ReferenceEquals(v1.GetPrivateField(), f1).IsTrue();

            var t1 = resolver.Resolve<ITransient1>() as Transient1;
            var t2 = resolver.Resolve<ITransient1>();
            Object.ReferenceEquals(t1, t2).IsFalse();

            t1.PublicField.IsNotNull();
            t1.PublicProperty.IsNotNull();
            t1.GetPrivateField().IsNotNull();
            t1.GetPrivateProperty().IsNotNull();
            t1.fromConstructor1.IsNotNull();
            t1.fromConstructor2.IsNotNull();
            t1.VerifyOkay();
        }

        [Fact]
        public void CollectionTest()
        {
            {
                var resolver = ObjectResolver.Create();
                resolver.RegisterCollection<IForCollection>(Lifestyle.Transient, typeof(Collection1), typeof(Collection2), typeof(Collection3));
                resolver.Compile();

                {
                    var collection1 = resolver.Resolve<IEnumerable<IForCollection>>() as IForCollection[];
                    collection1[0].IsInstanceOf<Collection1>();
                    collection1[1].IsInstanceOf<Collection2>();
                    collection1[2].IsInstanceOf<Collection3>();
                }
                {
                    var collection1 = resolver.Resolve<IReadOnlyList<IForCollection>>() as IForCollection[];
                    collection1[0].IsInstanceOf<Collection1>();
                    collection1[1].IsInstanceOf<Collection2>();
                    collection1[2].IsInstanceOf<Collection3>();
                }
                {
                    var collection1 = resolver.Resolve<IForCollection[]>() as IForCollection[];
                    collection1[0].IsInstanceOf<Collection1>();
                    collection1[1].IsInstanceOf<Collection2>();
                    collection1[2].IsInstanceOf<Collection3>();
                }
            }

            {
                var resolver = ObjectResolver.Create();
                resolver.RegisterCollection<IForCollection>(Lifestyle.Singleton, typeof(Collection1), typeof(Collection2), typeof(Collection3));
                resolver.Compile();

                {
                    var collection1 = resolver.Resolve<IEnumerable<IForCollection>>() as IForCollection[];
                    collection1[0].IsInstanceOf<Collection1>();
                    collection1[1].IsInstanceOf<Collection2>();
                    collection1[2].IsInstanceOf<Collection3>();

                    var collection2 = resolver.Resolve<IEnumerable<IForCollection>>() as IForCollection[];
                    (collection1 == collection2).IsTrue();
                }

                {
                    var collection1 = resolver.Resolve<IReadOnlyList<IForCollection>>() as IForCollection[];
                    collection1[0].IsInstanceOf<Collection1>();
                    collection1[1].IsInstanceOf<Collection2>();
                    collection1[2].IsInstanceOf<Collection3>();

                    var collection2 = resolver.Resolve<IReadOnlyList<IForCollection>>() as IForCollection[];
                    (collection1 == collection2).IsTrue();
                }
                {
                    var collection1 = resolver.Resolve<IForCollection[]>() as IForCollection[];
                    collection1[0].IsInstanceOf<Collection1>();
                    collection1[1].IsInstanceOf<Collection2>();
                    collection1[2].IsInstanceOf<Collection3>();

                    var collection2 = resolver.Resolve<IForCollection[]>() as IForCollection[];
                    (collection1 == collection2).IsTrue();
                }
            }
        }

        [Fact]
        public void NonGenericTest()
        {
            var resolver = ObjectResolver.Create();
            resolver.Register<ISingleton1, Singleton1>(Lifestyle.Singleton);
            resolver.Register<ITransient1, Transient1>(Lifestyle.Transient);
            resolver.Register<ICombined1, Combined1>(Lifestyle.Transient);
            resolver.Register<IFirstService, FirstService>(Lifestyle.Singleton);
            resolver.Register<ISecondService, SecondService>(Lifestyle.Singleton);
            resolver.Register<IThirdService, ThirdService>(Lifestyle.Singleton);
            resolver.Register<ISubObjectOne, SubObjectOne>(Lifestyle.Transient);
            resolver.Register<ISubObjectTwo, SubObjectTwo>(Lifestyle.Transient);
            resolver.Register<ISubObjectThree, SubObjectThree>(Lifestyle.Transient);
            resolver.Register<IComplex1, Complex1>(Lifestyle.Transient);
            resolver.RegisterCollection<IForCollection>(Lifestyle.Transient, typeof(Collection1), typeof(Collection2), typeof(Collection3));
            resolver.Compile();

            resolver.Resolve(typeof(ISingleton1)).IsInstanceOf<Singleton1>();
            resolver.Resolve(typeof(ITransient1)).IsInstanceOf<Transient1>();
            resolver.Resolve(typeof(ICombined1)).IsInstanceOf<Combined1>();
            resolver.Resolve(typeof(IFirstService)).IsInstanceOf<FirstService>();
            resolver.Resolve(typeof(ISecondService)).IsInstanceOf<SecondService>();
            resolver.Resolve(typeof(IThirdService)).IsInstanceOf<ThirdService>();
            resolver.Resolve(typeof(ISubObjectOne)).IsInstanceOf<SubObjectOne>();
            resolver.Resolve(typeof(ISubObjectTwo)).IsInstanceOf<SubObjectTwo>();
            resolver.Resolve(typeof(ISubObjectThree)).IsInstanceOf<SubObjectThree>();
            resolver.Resolve(typeof(IComplex1)).IsInstanceOf<Complex1>();

            var collection1 = (IForCollection[])resolver.Resolve(typeof(IEnumerable<IForCollection>));
            collection1[0].IsInstanceOf<Collection1>();
            collection1[1].IsInstanceOf<Collection2>();
            collection1[2].IsInstanceOf<Collection3>();

            var collection2 = (IForCollection[])resolver.Resolve(typeof(IForCollection[]));
            collection2[0].IsInstanceOf<Collection1>();
            collection2[1].IsInstanceOf<Collection2>();
            collection2[2].IsInstanceOf<Collection3>();

            var collection3 = (IForCollection[])resolver.Resolve(typeof(IReadOnlyList<IForCollection>));
            collection3[0].IsInstanceOf<Collection1>();
            collection3[1].IsInstanceOf<Collection2>();
            collection3[2].IsInstanceOf<Collection3>();
        }


        //[Fact]
        //public void NonGenericNotFoundTest()
        //{
        //    var resolver = ObjectResolver.Create();
        //    resolver.Register<IFirstService, FirstService>(Lifestyle.Singleton);
        //    resolver.Compile();

        //    resolver.Resolve(typeof(ITransient1));
        //}

        [Fact]
        public void ScopeTest()
        {
            var resolver = ObjectResolver.Create();
            resolver.Register<IFirstService, FirstService>(Lifestyle.Singleton);
            resolver.Register<ISecondService, SecondService>(Lifestyle.Transient);
            resolver.Register<IThirdService, ThirdService>(Lifestyle.Scoped);
            resolver.Compile();


            IThirdService sc1 = null;
            IThirdService sc2 = null;
            IThirdService sc3 = null;
            IThirdService sc4 = null;

            {
                using (var coResolver = resolver.BeginScope(ScopeProvider.Standard))
                {
                    var s1 = coResolver.Resolve<IFirstService>();
                    var s2 = coResolver.Resolve<IFirstService>();

                    var t1 = coResolver.Resolve<ISecondService>();
                    var t2 = coResolver.Resolve<ISecondService>();

                    sc1 = coResolver.Resolve<IThirdService>();
                    sc2 = coResolver.Resolve<IThirdService>();

                    Object.ReferenceEquals(s1, s2).IsTrue();
                    Object.ReferenceEquals(t1, t2).IsFalse();

                    Object.ReferenceEquals(sc1, sc2).IsTrue();


                    (sc1 as ThirdService).DisposeCount.Is(0);
                }

                (sc1 as ThirdService).DisposeCount.Is(1);
                sc1 = sc2 = sc3 = sc4 = null;
            }

            {
                using (var coResolver = resolver.BeginScope(ScopeProvider.ThreadLocal))
                {
                    var t1 = new Thread(_ =>
                    {
                        sc1 = coResolver.Resolve<IThirdService>();
                        sc2 = coResolver.Resolve<IThirdService>();
                    });
                    t1.Start();

                    var t2 = new Thread(_ =>
                    {
                        sc3 = coResolver.Resolve<IThirdService>();
                        sc4 = coResolver.Resolve<IThirdService>();
                    });
                    t2.Start();

                    t1.Join();
                    t2.Join();


                    Object.ReferenceEquals(sc1, sc2).IsTrue();
                    Object.ReferenceEquals(sc3, sc4).IsTrue();

                    Object.ReferenceEquals(sc1, sc3).IsFalse();

                    (sc1 as ThirdService).DisposeCount.Is(0);
                    (sc3 as ThirdService).DisposeCount.Is(0);
                }

                (sc1 as ThirdService).DisposeCount.Is(1);
                (sc3 as ThirdService).DisposeCount.Is(1);
                sc1 = sc2 = sc3 = sc4 = null;
            }

            {
                using (var coResolver = resolver.BeginScope(ScopeProvider.AsyncLocal))
                {
                    Task.Run(async () =>
                    {
                        sc1 = coResolver.Resolve<IThirdService>();
                        sc2 = await GetAsync(coResolver);
                    }).Wait();

                    Task.Run(async () =>
                    {
                        sc3 = await GetAsync(coResolver);
                        sc4 = coResolver.Resolve<IThirdService>();
                    }).Wait();

                    Object.ReferenceEquals(sc1, sc2).IsTrue();
                    Object.ReferenceEquals(sc3, sc4).IsFalse();

                    (sc1 as ThirdService).DisposeCount.Is(0);
                    (sc3 as ThirdService).DisposeCount.Is(0);
                }

                (sc1 as ThirdService).DisposeCount.Is(1);
                (sc3 as ThirdService).DisposeCount.Is(1);
                sc1 = sc2 = sc3 = sc4 = null;
            }
        }

        async Task<IThirdService> GetAsync(IObjectResolver resolver)
        {
            await Task.Yield();
            return resolver.Resolve<IThirdService>();
        }
    }
}
