﻿using System;
using System.Linq;
using System.Collections.Generic;

namespace PlatoCore.Modules.FileProviders
{
    public static class NormalizedPaths
    {
        /// <summary>
        /// Use a collection of file paths to resolve files and subfolders directly under a given folder.
        /// Paths need to be normalized by using '/' for the directory separator and with no leading '/'.
        /// </summary>
        public static void ResolveFolderContents(
            string folder,
            IEnumerable<string> normalizedPaths,
            out IEnumerable<string> filePaths,
            out IEnumerable<string> folderPaths)
        {
            var files = new HashSet<string>(StringComparer.Ordinal);
            var folders = new HashSet<string>(StringComparer.Ordinal);

            // Ensure a trailing slash.
            if (folder[folder.Length - 1] != '/')
            {
                folder = folder + '/';
            }

            foreach (var path in normalizedPaths.Where(a => a.StartsWith(folder, StringComparison.Ordinal)))
            {
                // Resolve the subpath relative to the folder.
                var subPath = path.Substring(folder.Length);
                var index = subPath.IndexOf('/');

                // If no more slash.
                if (index == -1)
                {
                    // It's a file.
                    files.Add(path);
                }
                else
                {
                    // Otherwise add the 1st subfolder path.
                    folders.Add(subPath.Substring(0, index));
                }
            }

            filePaths = files;
            folderPaths = folders;

        }

    }

}