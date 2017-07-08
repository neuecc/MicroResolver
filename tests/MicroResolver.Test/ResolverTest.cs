using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
