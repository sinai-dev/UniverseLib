using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UniverseLib.Utility
{
    public static class MiscUtility
    {
        /// <summary>
        /// Check if a string contains another string, case-insensitive.
        /// </summary>
        public static bool ContainsIgnoreCase(this string _this, string s)
        {
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf(_this, s, CompareOptions.IgnoreCase) >= 0;
        }

        /// <summary>
        /// Just to allow Enum to do .HasFlag() in NET 3.5
        /// </summary>
        public static bool HasFlag(this Enum flags, Enum value)
        {
            try
            {
                ulong flag = Convert.ToUInt64(value);
                return (Convert.ToUInt64(flags) & flag) == flag;
            }
            catch
            {
                long flag = Convert.ToInt64(value);
                return (Convert.ToInt64(flags) & flag) == flag;
            }
        }

        /// <summary>
        /// Returns true if the StringBuilder ends with the provided string.
        /// </summary>
        public static bool EndsWith(this StringBuilder sb, string _string)
        {
            int len = _string.Length;

            if (sb.Length < len)
                return false;

            int stringpos = 0;
            for (int i = sb.Length - len; i < sb.Length; i++, stringpos++)
            {
                if (sb[i] != _string[stringpos])
                    return false;
            }
            return true;
        }
    }
}
