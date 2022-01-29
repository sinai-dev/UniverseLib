using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace UniverseLib.Utility
{
    public static class IOUtility
    {
        private static readonly char[] invalidDirectoryCharacters = Path.GetInvalidPathChars();
        private static readonly char[] invalidFilenameCharacters = Path.GetInvalidFileNameChars();

        /// <summary>
        /// Ensures the path contains no invalid characters and that the containing directory exists.
        /// </summary>
        public static string EnsureValidFilePath(string fullPathWithFile)
        {
            // Remove invalid path characters
            fullPathWithFile = string.Concat(fullPathWithFile.Split(invalidDirectoryCharacters));

            // Create directory (does nothing if it exists)
            Directory.CreateDirectory(Path.GetDirectoryName(fullPathWithFile));

            return fullPathWithFile;
        }

        /// <summary>
        /// Ensures the file name contains no invalid characters.
        /// </summary>
        public static string EnsureValidFilename(string filename)
        {
            return string.Concat(filename.Split(invalidFilenameCharacters));
        }
    }
}
