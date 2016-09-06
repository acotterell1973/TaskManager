using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Extensions
{
    public static class ListExtensions
    {
        public static void AddAllWhere<TValue>(this List<TValue> list, List<TValue> source, Predicate<TValue> match)
        {
            list.AddRange(source.FindAll(match));
        }

        private static IEnumerable<T1> preserveOrderOf<T1, T2>(IList<T1> set, IList<T2> ordered, Func<T1, T2, bool> compare, bool preserveOrdinal)
        {
            if (preserveOrdinal)
            {
                if (ordered.Count != set.Count) throw new ArgumentException("Cannot preserve ordinals if set count and ordered count do not match");
            }

            bool[] used = new bool[set.Count];

            // Move forward according to the order of `ordered`:
            for (int i = 0; i < ordered.Count; ++i)
            {
                // Try to find a key match in the set:
                bool match = false;
                for (int j = 0; j < set.Count; ++j)
                {
                    // Move on if this set value has been used before so that
                    // we don't keep using the first duplicate set value for duplicates:
                    if (used[j]) continue;

                    // The keys must compare:
                    if (compare(set[j], ordered[i]))
                    {
                        // Mark the set value as used and indicate a match:
                        used[j] = true;
                        match = true;
                        // Yield the set value:
                        yield return set[j];
                        // Don't produce duplicates in this output slot:
                        break;
                    }
                }

                // Preserve the output set size by using default(T1) as a placeholder
                // for missing values:
                if (preserveOrdinal && !match)
                    yield return default(T1);
            }

            // If we're not interested in preserving the ordinal positions, then
            // append the remaining unused set values:
            if (!preserveOrdinal)
            {
                for (int j = 0; j < set.Count; ++j)
                {
                    if (used[j]) continue;
                    yield return set[j];
                }
            }
            yield break;
        }

        /// <summary>
        /// Preserves this set's ordering according to the order of keys in the <paramref name="ordered"/> set.
        /// Any items found in this set that are not in <paramref name="ordered"/> will be appended to the end
        /// in the order that they appear.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the set</typeparam>
        /// <typeparam name="TKey">The type of key from each object to compare for ordering</typeparam>
        /// <param name="set">The set of items to reorder</param>
        /// <param name="ordered">The set of keys whose ordering will be preserved</param>
        /// <param name="keySelector">A lambda to select keys for comparison between the two sets to determine the ordering</param>
        /// <returns></returns>
        public static IEnumerable<T> PreserveOrderOf<T, TKey>(this IList<T> set, IList<T> ordered, Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            var q = preserveOrderOf(set, ordered, (x, y) => keySelector(x).Equals(keySelector(y)), false);
            return q;
        }

        /// <summary>
        /// Preserves this set's ordering according to the order of keys in the <paramref name="ordered"/> set.
        /// Any items found in this set that are not in <paramref name="ordered"/> will be appended to the end
        /// in the order that they appear.
        /// </summary>
        /// <remarks>
        /// Duplicate keys are ordered in the order in which they appear in the set, which may or may not be
        /// accurate, but there is no way to guarantee same ordering in the face of duplicates.
        /// </remarks>
        /// <typeparam name="T">The type of objects contained in the set</typeparam>
        /// <typeparam name="TKey">The type of key from each object to compare for ordering</typeparam>
        /// <param name="set">The set of items to reorder</param>
        /// <param name="ordered">The set of keys whose ordering will be preserved</param>
        /// <param name="keySelector">A lambda to select keys for comparison between the two sets to determine the ordering</param>
        /// <returns></returns>
        public static IEnumerable<T> PreserveOrderOfKeys<T, TKey>(this IList<T> set, IList<TKey> ordered, Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            var q = preserveOrderOf(set, ordered, (x, y) => y.Equals(keySelector(x)), false);
            return q;
        }

        /// <summary>
        /// Assuming this set and <paramref name="ordered"/> are equivalent sets, produce an output
        /// set containing items from this set using the key ordering from <paramref name="ordered"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the set</typeparam>
        /// <typeparam name="TKey">The type of key from each object to compare for ordering</typeparam>
        /// <param name="set">The set of items to reorder</param>
        /// <param name="ordered">The set of keys whose ordering will be preserved</param>
        /// <param name="keySelector">A lambda to select keys for comparison between the two sets to determine the ordering</param>
        /// <returns></returns>
        public static IEnumerable<T> EquivalenceSetOrder<T, TKey>(this IList<T> set, IList<T> ordered, Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            var q = preserveOrderOf(set, ordered, (x, y) => keySelector(y).Equals(keySelector(x)), true);
            return q;
        }

        /// <summary>
        /// Assuming this set and <paramref name="ordered"/> are equivalent sets, produce an output
        /// set containing items from this set using the key ordering from <paramref name="ordered"/>.
        /// </summary>
        /// <typeparam name="T">The type of objects contained in the set</typeparam>
        /// <typeparam name="TKey">The type of key from each object to compare for ordering</typeparam>
        /// <param name="set">The set of items to reorder</param>
        /// <param name="ordered">The set of keys whose ordering will be preserved</param>
        /// <param name="keySelector">A lambda to select keys for comparison between the two sets to determine the ordering</param>
        /// <returns></returns>
        public static IEnumerable<T> EquivalenceSetOrderKeys<T, TKey>(this IList<T> set, IList<TKey> ordered, Func<T, TKey> keySelector)
            where TKey : IEquatable<TKey>
        {
            var q = preserveOrderOf(set, ordered, (x, y) => y.Equals(keySelector(x)), true);
            return q;
        }

        /// <summary>
        /// Sets all of the key/value pairs from <paramref name="items"/> into the current dictionary type.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dict"></param>
        /// <param name="items"></param>
        public static void MergeFrom<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var item in items)
            {
                dict[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Creates a delimited string out of an IEnumerable source. <typeparamref name="T"/>.ToString() is used to format the items as strings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source IEnumerable to convert to a delimited string</param>
        /// <param name="delimiter">The delimiter used to delimit the items from the source IEnumerable, such as ",".</param>
        /// <returns></returns>
        public static string ToDelimitedString<T>(this IEnumerable<T> source, string delimiter)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (delimiter == null) throw new ArgumentNullException("delimiter");
            StringBuilder sb = new StringBuilder();
            using (var en = source.GetEnumerator())
            {
                bool notdone = en.MoveNext();
                while (notdone)
                {
                    sb.Append(en.Current.ToString());
                    notdone = en.MoveNext();
                    if (notdone) sb.Append(delimiter);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Creates a comma delimited string out of an IEnumerable source. <typeparamref name="T"/>.ToString() is used to format the items as strings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source IEnumerable to convert to a delimited string</param>
        /// <returns></returns>
        public static string ToCommaDelimitedString<T>(this IEnumerable<T> source)
        {
            return source.ToDelimitedString(",");
        }

        /// <summary>
        /// Creates a delimited string out of an IEnumerable source using a converter delegate to format the items as strings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source IEnumerable to convert to a delimited string</param>
        /// <param name="delimiter">The delimiter used to delimit the items from the source IEnumerable, such as ",".</param>
        /// <param name="converter">The converter delegate used to convert the <typeparamref name="T"/> into a string</param>
        /// <returns></returns>
        public static string ToDelimitedString<T>(this IEnumerable<T> source, string delimiter, Func<T, string> converter)
        {
            StringBuilder sb = new StringBuilder();
            using (var en = source.GetEnumerator())
            {
                bool notdone = en.MoveNext();
                while (notdone)
                {
                    sb.Append(converter(en.Current));
                    notdone = en.MoveNext();
                    if (notdone) sb.Append(delimiter);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Creates a comma delimited string out of an IEnumerable source using a converter delegate to format the items as strings.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source IEnumerable to convert to a delimited string</param>
        /// <param name="converter">The converter delegate used to convert the <typeparamref name="T"/> into a string</param>
        /// <returns></returns>
        public static string ToCommaDelimitedString<T>(this IEnumerable<T> source, Func<T, string> converter)
        {
            return source.ToDelimitedString(",", converter);
        }

        /// <summary>
        /// If the source enumerable is a null reference, Enumerable.Empty() is returned. Otherwise, the original source is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<T> NullCoalesceAsEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null) return Enumerable.Empty<T>();
            return source;
        }

        public static TResult[] SelectAsArray<T, TResult>(this T[] arr, Func<T, TResult> projection)
        {
            var res = new TResult[arr.Length];
            for (int i = 0; i < arr.Length; ++i)
                res[i] = projection(arr[i]);
            return res;
        }

        public static TResult[] SelectAsArray<T, TResult>(this T[] arr, Func<T, int, TResult> projection)
        {
            var res = new TResult[arr.Length];
            for (int i = 0; i < arr.Length; ++i)
                res[i] = projection(arr[i], i);
            return res;
        }

        public static TResult[] SelectAsArray<T, TResult>(this ICollection<T> list, Func<T, TResult> projection)
        {
            var res = new TResult[list.Count];
            using (var en = list.GetEnumerator())
                for (int i = 0; en.MoveNext(); ++i)
                    res[i] = projection(en.Current);
            return res;
        }

        public static List<TResult> SelectAsList<T, TResult>(this ICollection<T> list, Func<T, TResult> projection)
        {
            var res = new List<TResult>(list.Count);
            res.AddRange(list.Select(projection));
            return res;
        }

        public static List<TResult> DistinctListOf<T, TResult>(this ICollection<T> list, Func<T, TResult> selector, int? cardinality = null)
        {
            var res = new List<TResult>(cardinality ?? list.Count);
            res.AddRange(list.Select(selector).Distinct());
            return res;
        }


        public static T[] Slice<T>(this T[] arr, int startIndex)
        {
            if (arr == null) return null;

            return Slice(arr, startIndex, arr.Length - startIndex);
        }

        public static T[] Slice<T>(this T[] arr, int startIndex, int length)
        {
            if (arr == null) return null;

            if (startIndex < 0) throw new ArgumentOutOfRangeException("startIndex");

            // 0-length slice returns an empty array:
            if (length == 0) return new T[0];

            if (startIndex >= arr.Length) throw new ArgumentOutOfRangeException("startIndex");

            // Negative length is relative to array's length:
            if (length < 0) length = (arr.Length - startIndex) + length;

            // Total length is out of range?
            if (length > arr.Length) throw new ArgumentOutOfRangeException("length");

            // Take the slice:
            T[] slice = new T[length];
            Array.Copy(arr, startIndex, slice, 0, length);
            return slice;
        }

        public static T[] ToArray<T>(this IEnumerable<T> src, int length)
        {
            T[] arr = new T[length];
            using (IEnumerator<T> e = src.GetEnumerator())
            {
                for (int i = 0; e.MoveNext(); ++i)
                {
                    arr[i] = e.Current;
                }
                return arr;
            }
        }

        public static T[] AppendAsArray<T>(this T[] src, T element)
        {
            T[] arr = new T[src.Length + 1];
            Array.Copy(src, arr, src.Length);
            arr[src.Length] = element;
            return arr;
        }

        public static T[] AppendAsArray<T>(this IEnumerable<T> src, T element, int length)
        {
            T[] arr = new T[length + 1];
            using (IEnumerator<T> e = src.GetEnumerator())
            {
                for (int i = 0; e.MoveNext(); ++i)
                {
                    arr[i] = e.Current;
                }
                arr[length] = element;
                return arr;
            }
        }

        public static List<T> ToList<T>(this IEnumerable<T> src, int initialCapacity)
        {
            List<T> lst = new List<T>(initialCapacity);
            using (IEnumerator<T> e = src.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    lst.Add(e.Current);
                }
                return lst;
            }
        }

        /// <summary>
        /// Uses ThreadPool.QueueUserWorkItem to schedule a background thread that enumerates the query and
        /// builds a List&lt;<typeparamref name="T"/>&gt; to hold the results.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">Query to enumerate on the background thread</param>
        /// <param name="completion">Action to handle the results of the query; executed on the background thread</param>
        /// <param name="initialCapacity">An expected count of results from the query; used for the initial capacity of List</param>
        /// <returns>A WaitHandle that will be signaled on completion of the query enumeration.</returns>
        /// <remarks>
        /// Enumerating the results of <paramref name="query"/> should be side-effect free and should be safe to execute on a separate thread.
        /// </remarks>
        public static System.Threading.WaitHandle BackgroundEnumerate<T>(this IEnumerable<T> query, Action<List<T>> completion, int initialCapacity)
        {
            var ev = new System.Threading.ManualResetEvent(false);

            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    var results = new List<T>(initialCapacity);
                    results.AddRange(query);

                    completion(results);
                }
                finally
                {
                    ev.Set();
                }
            });

            return ev;
        }
    }
}
