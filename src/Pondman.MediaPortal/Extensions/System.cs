using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class SystemExtensions
    {
        /// <summary>
        /// Determines whether the specified obj is null.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="obj">The obj.</param>
        /// <returns>
        ///   <c>true</c> if the specified obj is null; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNull<TObject>(this TObject obj)
            where TObject : class
        {
            return (obj != null);
        }

        /// <summary>
        /// Executes the handler if the object is not null
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="obj">The obj.</param>
        /// <param name="handler">The handler.</param>
        public static void IfNotNull<TObject>(this TObject obj, Action<TObject> handler)
            where TObject : class
        {
            if (!obj.IsNull()) handler(obj);
        }

        /// <summary>
        /// Determines whether [is null or white space] [the specified STR].
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>
        ///   <c>true</c> if [is null or white space] [the specified STR]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return str.IsNull() || String.IsNullOrEmpty(str.Trim());
        }

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

        /// <summary>
        /// Creates an MD5 hash
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>hash</returns> 
        public static string ToMd5Hash(this string input)
        {
            System.Security.Cryptography.MD5 _md5Hasher = System.Security.Cryptography.MD5.Create();
            byte[] data = _md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
            return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Fires the event.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <param name="onError">The on error.</param>
        public static void FireEvent(this EventHandler handler, object sender, EventArgs args, Action<Exception> onError = null)
        {
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex)
                {
                    if (onError != null) onError(ex);
                }
            }
        }

        /// <summary>
        /// Fires the event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler">The handler.</param>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The args.</param>
        /// <param name="onError">The on error.</param>
        public static void FireEvent<T>(this EventHandler<T> handler, object sender, T args, Action<Exception> onError = null)
           where T : EventArgs
        {
            if (handler != null)
            {
                try
                {
                    handler(sender, args);
                }
                catch (Exception ex)
                {
                    if (onError != null) onError(ex);
                }
            }
        }
    }
}
