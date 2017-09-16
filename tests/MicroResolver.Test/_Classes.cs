#pragma warning disable 169, 649

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MicroResolver.Test
{
    public interface ISingleton1
    {
    }

    public class Singleton1 : ISingleton1
    {
        bool calledConstructor;
        bool calledMethod1;
        bool calledMethod2;

        IComplex1 privateProperty;
        IThirdService publicProperty;

        [Inject]
        IFirstService PrivateField;

        [Inject]
        public ISecondService PublicField;

        [Inject]
        IComplex1 PrivateProperty
        {
            set
            {
                privateProperty = value;
            }
        }

        [Inject]
        public IThirdService PublicProperty
        {
            get
            {
                return publicProperty;
            }
            set
            {
                publicProperty = value;
            }
        }

        public ISubObjectOne fromConstructor1;
        public ISubObjectTwo fromConstructor2;

        public Singleton1(int x, int y, int z)
        {

        }

        [Inject]
        public Singleton1(ISubObjectOne one, ISubObjectTwo two)
        {
            calledConstructor = true;
            fromConstructor1 = one;
            fromConstructor2 = two;
        }

        [Inject]
        public void DoSomething1()
        {
            calledMethod1 = true;
        }

        [Inject]
        public void DoSomething2()
        {
            calledMethod2 = true;
        }

        public IFirstService GetPrivateField()
        {
            return PrivateField;
        }

        public IComplex1 GetPrivateProperty()
        {
            return privateProperty;
        }

        public bool VerifyOkay()
        {
            if (calledConstructor && calledMethod1 && calledMethod2) return true;
            return false;
        }
    }

    public interface ITransient1
    {
    }

    public class Transient1 : ITransient1
    {
        bool calledConstructor;
        bool calledMethod1;
        bool calledMethod2;

        IComplex1 privateProperty;
        IThirdService publicProperty;

        [Inject]
        IFirstService PrivateField;

        [Inject]
        public ISecondService PublicField;

        [Inject]
        IComplex1 PrivateProperty
        {
            set
            {
                privateProperty = value;
            }
        }

        [Inject]
        public IThirdService PublicProperty
        {
            get
            {
                return publicProperty;
            }
            set
            {
                publicProperty = value;
            }
        }

        public ISubObjectOne fromConstructor1;
        public ISubObjectTwo fromConstructor2;

        public Transient1(int x, int y, int z)
        {

        }

        [Inject]
        public Transient1(ISubObjectOne one, ISubObjectTwo two)
        {
            calledConstructor = true;
            fromConstructor1 = one;
            fromConstructor2 = two;
        }

        [Inject]
        public void DoSomething1()
        {
            calledMethod1 = true;
        }

        [Inject]
        public void DoSomething2()
        {
            calledMethod2 = true;
        }

        public IFirstService GetPrivateField()
        {
            return PrivateField;
        }

        public IComplex1 GetPrivateProperty()
        {
            return privateProperty;
        }

        public bool VerifyOkay()
        {
            if (calledConstructor && calledMethod1 && calledMethod2) return true;
            return false;
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
        }
    }



    public interface ICombined12
    {
        void DoSomething();
    }

    public class Combined12 : ICombined12
    {
        public Combined12(ISingleton1 first, ITransient1 second)
        {
        }

        public void DoSomething()
        {
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
        private static readonly int staticInitialzierRequiredField = 1;

        public SecondService()
        {
        }
    }

    public interface IThirdService
    {
    }

    public class ThirdService : IThirdService, IDisposable
    {
        public int DisposeCount;

        public ThirdService()
        {
        }

        public void Dispose()
        {
            Interlocked.Increment(ref DisposeCount);
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
