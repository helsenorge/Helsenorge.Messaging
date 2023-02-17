/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.IO;
using System.Reflection;

namespace Helsenorge.Registries.Tests
{
    internal static class TestFileUtility
    {
        // Gets the full path to Relative Unit Testing File
        internal static string GetFullPathToFile(string pathRelativeUnitTestingFile)
        {
            string folderProjectLevel = GetPathToCurrentUnitTestProject();
            string path = Path.Combine(folderProjectLevel, pathRelativeUnitTestingFile);
            return path;
        }

        // Get the path to the current unit testing project.
        private static string GetPathToCurrentUnitTestProject()
        {
            string pathAssembly = Assembly.GetExecutingAssembly().Location;
            string folderAssembly = Path.GetDirectoryName(pathAssembly);
            if (folderAssembly.EndsWith(Path.DirectorySeparatorChar) == false) folderAssembly += Path.DirectorySeparatorChar;
            string path = Path.GetFullPath(folderAssembly + $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}");
            return path;
        }
    }
}
