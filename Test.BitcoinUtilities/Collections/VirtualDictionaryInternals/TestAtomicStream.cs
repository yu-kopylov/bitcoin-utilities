using System.IO;
using BitcoinUtilities.Collections.VirtualDictionaryInternals;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Collections.VirtualDictionaryInternals
{
    [TestFixture]
    public class TestAtomicStream
    {
        //todo: Test some seek specific behaviour (compare with standard streams). For example write far beyound the end of a file.

        [Test]
        public void TestSimple()
        {
            MemoryStream mainStream = new MemoryStream();
            MemoryStream walStream = new MemoryStream();

            using (AtomicStream stream = new AtomicStream(mainStream, walStream))
            {
                stream.Write(new byte[]{1}, 0, 1);
                stream.Commit();
            }

            byte[] mainContent = mainStream.ToArray();

            Assert.That(mainContent.Length, Is.EqualTo(1));
            Assert.That(mainContent[0], Is.EqualTo(1));
        }
    }
}