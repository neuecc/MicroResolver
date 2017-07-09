#pragma warning disable 169

using MicroResolver;
using MicroResolver.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerationDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = typeof(Program);
            var huga = t.GetHashCode();

            var t2 = typeof(IInterface);
            var aaa = t  == t2;

            var resolver = ObjectResolver.Create();
            //resolver.Register<IInterface, DummyClass>(Lifestyle.Singleton);
            //resolver.Register<Moge, Moge>(Lifestyle.Transient);

            //resolver.Register<ISingleton1, Singleton1>(Lifestyle.Singleton);
            //resolver.Register<ITransient1, Transient1>(Lifestyle.Transient);
            //resolver.Register<ICombined1, Combined1>(Lifestyle.Transient);

            resolver.Register<IFirstService, FirstService>(Lifestyle.Singleton);
            //resolver.Register<ISecondService, SecondService>(Lifestyle.Singleton);
            //resolver.Register<IThirdService, ThirdService>(Lifestyle.Singleton);
            //resolver.Register<ISubObjectOne, SubObjectOne>(Lifestyle.Transient);
            //resolver.Register<ISubObjectTwo, SubObjectTwo>(Lifestyle.Transient);
            //resolver.Register<ISubObjectThree, SubObjectThree>(Lifestyle.Transient);

            //resolver.Register<IComplex1, Complex1>(Lifestyle.Transient);

            //resolver.RegisterCollection<IForCollection>(Lifestyle.Transient, typeof(Collection1), typeof(Collection2), typeof(Collection3));

            //var verify = resolver.DebuggingCompile();
            //Verify(verify);


            resolver.Compile();

            //var test = resolver.Resolve<ICombined1>();
            //Console.WriteLine(test);
        }

        static void Verify(params AssemblyBuilder[] builders)
        {
            var path = @"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\x64\PEVerify.exe";

            foreach (var targetDll in builders)
            {
                var psi = new ProcessStartInfo(path, targetDll.GetName().Name + ".dll")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                var p = Process.Start(psi);
                var data = p.StandardOutput.ReadToEnd();
                Console.WriteLine(data);
            }
        }
    }

    public interface IInterface
    {

    }

    public class ParentClass
    {
        [Inject]
        public Moge ParentPublicProperty { get; set; }
        [Inject]
        public Moge ParentPrivateProperty
        {
            set
            {
            }
        }

        [Inject]
        public Moge ParentPublicField;
        [Inject]
        protected Moge ParentProtectedField;
        [Inject]
        Moge ParentPrivateField;

        [Inject]
        public void ParentPublicMethod()
        {
        }

        [Inject]
        protected void ParentProtectedMethod()
        {
        }

        [Inject]
        void ParentPrivateMethod()
        {
        }
    }

    public class DummyClass : ParentClass, IInterface
    {
        [Inject]
        public Moge PublicProperty { get; set; }
        [Inject]
        public Moge PrivateProperty
        {
            set
            {
            }
        }

        [Inject]
        public Moge PublicField;
        [Inject]
        Moge PrivateField;

        DummyClass()
        {
            Console.WriteLine("called private constructor");
        }

        [Inject]
        public DummyClass(Moge x)
        {

        }


        [Inject]
        public void PublicMethod()
        {
            Console.WriteLine(PrivateField);
        }

        [Inject]
        void PrivateMethod()
        {
            Console.WriteLine("Private Method Called!");
        }
    }

    public interface IInjectable
    {
    }

    public class Moge : IInjectable
    {
        //public Moge()
        //{

        //}
        //[Inject]
        public IInterface MyProperty { get; set; }

        [Inject]
        public void HugaHuga()
        {
            Console.WriteLine("Yeah! Moge Injected!");
        }
    }

    public class ImageOfImage
    {

    }

    public interface ISingleton1
    {
        void DoSomething();
    }
    public class Singleton1 : ISingleton1
    {
        public Singleton1()
        {
        }

        public void DoSomething()
        {
            Console.WriteLine("Hello");
        }
    }

    public interface ITransient1
    {
        void DoSomething();
    }

    public class Transient1 : ITransient1
    {
        public Transient1()
        {
        }

        public void DoSomething()
        {
            Console.WriteLine("World");
        }
    }

    public interface ICombined1
    {
        void DoSomething();
    }

    public class Combined1 : ICombined1
    {
        public Combined1(ISingleton1 first, ITransient1 second)
        {
        }

        public void DoSomething()
        {
            Console.WriteLine("Combined");
        }
    }
    public interface IComplex1
    {
    }

    public class Complex1 : IComplex1
    {
        private static int counter;
        public Complex1(
            IFirstService firstService,
            ISecondService secondService,
            IThirdService thirdService,
            ISubObjectOne subObjectOne,
            ISubObjectTwo subObjectTwo,
            ISubObjectThree subObjectThree)
        {
            if (firstService == null)
            {
                throw new ArgumentNullException(nameof(firstService));
            }

            if (secondService == null)
            {
                throw new ArgumentNullException(nameof(secondService));
            }

            if (thirdService == null)
            {
                throw new ArgumentNullException(nameof(thirdService));
            }

            if (subObjectOne == null)
            {
                throw new ArgumentNullException(nameof(subObjectOne));
            }

            if (subObjectTwo == null)
            {
                throw new ArgumentNullException(nameof(subObjectTwo));
            }

            if (subObjectThree == null)
            {
                throw new ArgumentNullException(nameof(subObjectThree));
            }

            System.Threading.Interlocked.Increment(ref counter);
        }

        public static int Instances
        {
            get { return counter; }
            set { counter = value; }
        }
    }

    public interface IFirstService
    {
    }

    public class FirstService : IFirstService
    {
        public FirstService()
        {
        }
    }

    public interface ISecondService
    {
    }

    public class SecondService : ISecondService
    {
        public SecondService()
        {
        }
    }

    public interface IThirdService
    {
    }

    public class ThirdService : IThirdService
    {
        public ThirdService()
        {
        }
    }

    public interface ISubObjectOne
    {
    }

    public class SubObjectOne : ISubObjectOne
    {
        public SubObjectOne(IFirstService firstService)
        {
            if (firstService == null)
            {
                throw new ArgumentNullException(nameof(firstService));
            }
        }
    }
    public interface ISubObjectTwo
    {
    }

    public class SubObjectTwo : ISubObjectTwo
    {
        public SubObjectTwo(ISecondService secondService)
        {
            if (secondService == null)
            {
                throw new ArgumentNullException(nameof(secondService));
            }
        }
    }
    public interface ISubObjectThree
    {
    }

    public class SubObjectThree : ISubObjectThree
    {
        public SubObjectThree(IThirdService thirdService)
        {
            if (thirdService == null)
            {
                throw new ArgumentNullException(nameof(thirdService));
            }
        }
    }


    public interface IForCollection
    {

    }

    public class Collection1 : IForCollection
    {

    }

    public class Collection2 : IForCollection
    {

    }

    public class Collection3 : IForCollection
    {

    }
}