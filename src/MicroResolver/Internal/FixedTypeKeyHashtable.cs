using System;
using System.Collections.Generic;

namespace MicroResolver.Internal
{
    internal class FixedTypeKeyHashtable<TValue>
    {
        HashTuple[][] table;

        public FixedTypeKeyHashtable(KeyValuePair<Type, TValue>[] values)
        {
            var capacity = (double)((float)values.Length / 0.72f);
            capacity = (capacity > 3.0) ? HashHelper.GetPrime((int)capacity) : 3;
            table = new HashTuple[(int)capacity][];

            foreach (var item in values)
            {
                var hash = item.Key.GetHashCode();
                var array = table[hash % table.Length];
                if (array == null)
                {
                    array = new HashTuple[1];
                    array[0] = new HashTuple() { type = item.Key, value = item.Value };
                }
                else
                {
                    var newArray = new HashTuple[array.Length + 1];
                    Array.Copy(array, newArray, array.Length);
                    array = newArray;
                    array[array.Length - 1] = new HashTuple() { type = item.Key, value = item.Value };
                }

                table[hash % table.Length] = array;
            }
        }

        public TValue Get(Type type)
        {
            var hashCode = type.GetHashCode();
            var buckets = table[hashCode % table.Length];

            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i].type == type)
                {
                    return buckets[i].value;
                }
            }

            throw new MicroResolverException("Type was not dound, Type: " + type.FullName);
        }

        public bool TryGet(Type type, out TValue value)
        {
            var hashCode = type.GetHashCode();
            var buckets = table[hashCode % table.Length];

            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i].type == type)
                {
                    value = buckets[i].value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        struct HashTuple
        {
            public Type type;
            public TValue value;

            public override string ToString()
            {
                return (type == null) ? "null" : type.FullName;
            }
        }
    }

    internal static class HashHelper
    {
        public static int GetPrime(int min)
        {
            for (int i = 0; i < primes.Length; i++)
            {
                int num = primes[i];
                if (num >= min)
                {
                    return num;
                }
            }
            for (int j = min | 1; j < 2147483647; j += 2)
            {
                if (IsPrime(j) && (j - 1) % 101 != 0)
                {
                    return j;
                }
            }
            return min;
        }

        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int num = (int)Math.Sqrt((double)candidate);
                for (int i = 3; i <= num; i += 2)
                {
                    if (candidate % i == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            return candidate == 2;
        }

        static readonly int[] primes = new int[]
        {
            3,
            7,
            11,
            17,
            23,
            29,
            37,
            47,
            59,
            71,
            89,
            107,
            131,
            163,
            197,
            239,
            293,
            353,
            431,
            521,
            631,
            761,
            919,
            1103,
            1327,
            1597,
            1931,
            2333,
            2801,
            3371,
            4049,
            4861,
            5839,
            7013,
            8419,
            10103,
            12143,
            14591,
            17519,
            21023,
            25229,
            30293,
            36353,
            43627,
            52361,
            62851,
            75431,
            90523,
            108631,
            130363,
            156437,
            187751,
            225307,
            270371,
            324449,
            389357,
            467237,
            560689,
            672827,
            807403,
            968897,
            1162687,
            1395263,
            1674319,
            2009191,
            2411033,
            2893249,
            3471899,
            4166287,
            4999559,
            5999471,
            7199369
        };
    }
}
