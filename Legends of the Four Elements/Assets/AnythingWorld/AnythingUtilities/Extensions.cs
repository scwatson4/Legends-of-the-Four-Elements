using System.Collections.Generic;
using System.Linq;
namespace AnythingWorld.Utilities
{
    /// <summary>
    /// Provides extension methods for working with enumerables.
    /// </summary>
    public static class EnumExtension
    {
        /// <summary>
        /// Returns an enumerable collection of tuples, where each tuple contains an item
        /// from the input collection paired with its corresponding index.
        /// </summary>
        /// <typeparam name="T">The type of the items in the input collection.</typeparam>
        /// <param name="self">The input collection.</param>
        /// <returns>An enumerable collection of tuples.</returns>
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> self)
            => self.Select((item, index) => (item, index));
    }

    /// <summary>
    /// Provides extension methods for working with lists.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Divides a list into a list of sub-lists, where each sub-list contains up to the specified
        /// number of items.
        /// </summary>
        /// <typeparam name="T">The type of the items in the input list.</typeparam>
        /// <param name="source">The input list.</param>
        /// <param name="chunkSize">The maximum number of items in each sub-list.</param>
        /// <returns>A list of sub-lists.</returns>
        public static List<List<T>> ChunkBy<T>(this List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}



