using System;
using System.Text;
using System.Linq;
using System.Reflection;

namespace Sb49.Common
{
    public static class Util
    {
        public static string GetTagInfo(object source, string suffix = null)
        {
            var typeName = source?.GetType().Name;
            return string.Format("{0}{1}", typeName, suffix);
        }

        public static string ToCapital(this string src)
        {
            if (string.IsNullOrEmpty(src))
                return src;

            if (src.Length == 1)
                return src.ToUpper();
            return src.Substring(0, 1).ToUpper() + src.Substring(1).ToLower();
        }

        public static bool HasFlags(int flags, int flag)
        {
            return (flags & flag) == flag;
        }

        public static string ToHex(byte[] bytes, bool upperCase)
        {
            var result = new StringBuilder(bytes.Length * 2);

            foreach (var t in bytes)
            {
                result.Append(t.ToString(upperCase ? "X2" : "x2"));
            }

            return result.ToString();
        }

        #region Exception

        public static Exception FindException(Exception ex, params Type[] findTypes)
        {
            if (ex == null || findTypes == null || findTypes.Length == 0)
                return null;

            bool EqualsHandler(Exception exc)
            {
                if (exc == null)
                    return false;
                var exctype = exc.GetType();
                return findTypes.Contains(exctype) || findTypes.Any(p => p.IsAssignableFrom(exctype));
            }

            if (EqualsHandler(ex))
                return ex;

            var innerException = ex.InnerException;
            return EqualsHandler(innerException) ? innerException : FindException(innerException, findTypes);
        }

        /// <summary>
        /// Преобразование исключения в строку. Использование этого метода обеспечит единый формат вывода.
        /// </summary>
        public static string ExceptionToString(Exception ex, bool includeExceptionType = true, int ident = 1,
            bool useNewLine = false)
        {
            return ExceptionToString(ex, includeExceptionType, ident, useNewLine ? Environment.NewLine : null);
        }

        private static string ExceptionToString(Exception ex, bool includeExceptionType, int ident, string newline)
        {
            if (ex == null)
                return string.Empty;

            string GetExeptionTypeHandler(Exception exinternal) => exinternal != null && includeExceptionType ? exinternal.GetType().FullName + ": " : null;

            var pref = ident == 0
                ? string.Empty
                : string.Format("{0}{1}{0}", newline == Environment.NewLine ? null : " ", new string('-', ident) + ">");

            var message = string.Format("{0}{1}", GetExeptionTypeHandler(ex), ex.Message);
            var aggEx = ex as AggregateException;
            if (aggEx != null)
            {
                message = string.Empty;
                // если нет пояснений, выведем, что есть
                if (aggEx.InnerExceptions == null || aggEx.InnerExceptions.Count == 0)
                    return string.Format("{0}{1}{2}", pref, GetExeptionTypeHandler(aggEx), aggEx.Message);

                // в общем случае может быть более одной ошибки
                foreach (var item in aggEx.InnerExceptions)
                {
                    message +=
                        string.Format("{0}{1}{2}", string.IsNullOrEmpty(message) ? null : newline, pref,
                            ExceptionToString(item, includeExceptionType, ident, newline));
                }

                return message;
            }

            if (ex.InnerException != null)
            {
                message +=
                    string.Format("{0}{1}{2}", string.IsNullOrEmpty(message) ? string.Empty : newline, pref,
                        ExceptionToString(ex.InnerException, includeExceptionType, ident + 1, newline));
            }

            return message;
        }

        #endregion Exception
    }
}
