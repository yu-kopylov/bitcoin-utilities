using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace TestUtilities
{
    public class TestUtils
    {
        /// <summary>
        /// Creates folder for tests which name is based on type of the test and a given relative path.
        /// <para/>
        /// Removes files matching given patterns from the test folder.
        /// </summary>
        /// <param name="testType">The type of the test.</param>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="patterns">The patterns of files that should be deleted.</param>
        /// <returns>A full path to the test folder.</returns>
        public static string PrepareTestFolder(Type testType, [CallerMemberName] string relativePath = "Test", params string[] patterns)
        {
            string testFolder = Path.GetFullPath(Path.Combine("tmp-test", testType.Name, relativePath));

            System.Diagnostics.Debug.WriteLine($"Removing files in the test folder: {testFolder}");

            Directory.CreateDirectory(testFolder);
            foreach (string pattern in patterns)
            {
                foreach (string filename in Directory.GetFiles(testFolder, pattern, SearchOption.TopDirectoryOnly))
                {
                    System.Diagnostics.Debug.WriteLine($"Removing: {filename}");
                    File.Delete(filename);
                }
            }

            return testFolder;
        }

        /// <summary>
        /// Creates folder for tests which name is based on type of the test and test method name.
        /// <para/>
        /// Removes files matching given patterns from the test folder.
        /// </summary>
        /// <param name="patterns">The patterns of files that should be deleted.</param>
        /// <returns>A full path to the test folder.</returns>
        public static string PrepareTestFolder(params string[] patterns)
        {
            var method = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod();
            return PrepareTestFolder(method.ReflectedType, method.Name, patterns);
        }
    }
}