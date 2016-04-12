using System;
using System.IO;

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
        public static string PrepareTestFolder(Type testType, string relativePath, params string[] patterns)
        {
            string testFolder = Path.GetFullPath(Path.Combine("tmp-test", testType.Name, relativePath));

            Console.WriteLine("Removing files in the test folder: : {0}", testFolder);

            Directory.CreateDirectory(testFolder);
            foreach (string pattern in patterns)
            {
                foreach (string filename in Directory.GetFiles(testFolder, pattern, SearchOption.TopDirectoryOnly))
                {
                    Console.WriteLine("Removing: {0}", filename);
                    File.Delete(filename);
                }
            }

            return testFolder;
        }
    }
}