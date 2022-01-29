using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniverseLib.Utility
{
    public static class ArgumentUtility
    {
        /// <summary>
        /// Equivalent to <c>new Type[0]</c>
        /// </summary>
        public static readonly Type[] EmptyTypes = new Type[0];

        /// <summary>
        /// Equivalent to <c>new object[0]</c>
        /// </summary>
        public static readonly object[] EmptyArgs = new object[0];

        /// <summary>
        /// Equivalent to <c>new Type[] { typeof(string) }</c>
        /// </summary>
        public static readonly Type[] ParseArgs = new Type[] { typeof(string) };
    }
}
