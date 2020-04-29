using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector {
    
    public static class CollectionExtensions {

        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> input) {
            foreach(var i in input) {
                set.Add(i);
            }
        }

        public static void AddRange<T, H>(this HashSet<T> set, IEnumerable<H> input, Func<H, T> mapper) {
            foreach (var i in input) {
                set.Add(mapper(i));
            }
        }

        public static void AddRange<K, V>(this IDictionary<K, V> dict, IEnumerable<V> input, Func<V, K> mapper) {
            foreach (var i in input) {
                dict[mapper(i)] = i;
            }
        }

    }

}
