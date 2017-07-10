using System;
using System.Collections.Generic;

namespace MicroResolver.Internal
{
    internal class FixedTypeKeyHashtable<TValue>
    {
        internal HashTuple[][] table;
        internal int indexFor;

        static readonly int MaximumCapacity = 1 << 30;

        public FixedTypeKeyHashtable(KeyValuePair<Type, TValue>[] values, float loadFactor = 0.75f)
        {
            var initialCapacity = (int)((float)values.Length / loadFactor);
            if (initialCapacity > MaximumCapacity)
            {
                initialCapacity = MaximumCapacity;
            }

            var capacity = 1;
            while (capacity < initialCapacity)
            {
                capacity <<= 1;
            }

            table = new HashTuple[(int)capacity][];
            indexFor = table.Length - 1;

            foreach (var item in values)
            {
                var hash = item.Key.GetHashCode();
                var array = table[hash & indexFor];
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

                table[hash & indexFor] = array;
            }
        }

        public TValue Get(Type type)
        {
            var hashCode = type.GetHashCode();
            var buckets = table[hashCode & indexFor];

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
            var buckets = table[hashCode & indexFor];

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

        internal struct HashTuple
        {
            public Type type;
            public TValue value;

            public override string ToString()
            {
                return (type == null) ? "null" : type.FullName;
            }
        }
    }
}
