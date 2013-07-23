using System.Collections.Generic;
using System.Linq;

namespace System
{
    public static class StringExtensions
    {
        /// <summary>
        /// Formats the specified string with the given arguments.
        /// </summary>
        /// <param name="str">the inpout string</param>
        /// <param name="args">The args.</param>
        /// <returns></returns>
        public static string Format(this string str, params object[] args)
        {
            return string.Format(str, args);
        }

        /// <summary>
        /// Converts the ienumerable to a delimited string
        /// </summary>
        /// <param name="str">the collection of strings</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns></returns>
        public static string ToDelimited(this IEnumerable<string> str, string delimiter = ", ") 
        {
            return string.Join(delimiter, str.ToArray());
        }

        /// <summary>
        /// Converts the ienumerable to a delimited string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <returns></returns>
        public static string ToDelimited<T>(this IEnumerable<T> list, Func<T, string> selector, string delimiter = ", ")
        {
            return string.Join(delimiter, list.Select(selector).ToArray());
        }
    }
}
