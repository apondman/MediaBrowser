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
            return (obj == null);
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

        public static bool IsIn(this string str, params string[] args)
        {
            return args.Contains(str);
        }

        public static void SafeInvoke<T1>(this Action<T1> obj, T1 arg)
        {
            obj.IfNotNull(handler => handler(arg));
        }

        public static void SafeInvoke<T1,T2>(this Action<T1,T2> obj, T1 p1, T2 p2)
        {
            obj.IfNotNull(handler => handler(p1,p2));
        }

        public static void SafeInvoke<T1, T2, T3>(this Action<T1, T2, T3> obj, T1 p1, T2 p2, T3 p3)
        {
            obj.IfNotNull(handler => handler(p1, p2, p3));
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
            Security.Cryptography.MD5 hasher = Security.Cryptography.MD5.Create();
            byte[] data = hasher.ComputeHash(Encoding.Default.GetBytes(input));
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
            if (handler == null) return;
            try
            {
                handler(sender, args);
            }
            catch (Exception ex)
            {
                onError.SafeInvoke(ex);
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
                    onError.SafeInvoke(ex);
                }
            }
        }

    }
}
