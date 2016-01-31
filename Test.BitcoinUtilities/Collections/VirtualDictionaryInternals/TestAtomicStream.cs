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
                stream.Write(new byte[] {1}, 0, 1);
                stream.Commit();

                Assert.That(mainStream.ToArray(), Is.EqualTo(new byte[] {1}));

                stream.Write(new byte[] {2}, 0, 1);
                stream.Commit();

                Assert.That(mainStream.ToArray(), Is.EqualTo(new byte[] {1, 2}));

                stream.Position = 0;
                stream.Write(new byte[] {3}, 0, 1);
                stream.Commit();

                Assert.That(mainStream.ToArray(), Is.EqualTo(new byte[] {3, 2}));
            }
        }

        [Test]
        public void TestSequentialRead()
        {
            MemoryStream mainStream = new MemoryStream();
            MemoryStream walStream = new MemoryStream();

            using (AtomicStream stream = new AtomicStream(mainStream, walStream))
            {
                stream.Write(new byte[] {1, 2, 3, 4, 5}, 0, 5);
                stream.Commit();

                byte[] buffer = new byte[3];
                stream.Position = 0;

                Assert.That(stream.Read(buffer, 0, 3), Is.EqualTo(3));
                Assert.That(buffer, Is.EqualTo(new byte[] {1, 2, 3}));

                Assert.That(stream.Read(buffer, 0, 3), Is.EqualTo(2));
                Assert.That(buffer, Is.EqualTo(new byte[] {4, 5, 3}));

                Assert.That(stream.Read(buffer, 0, 3), Is.EqualTo(0));
                Assert.That(buffer, Is.EqualTo(new byte[] {4, 5, 3}));
            }
        }

        [Test]
        public void TestReadWriteWithOffset()
        {
            MemoryStream mainStream = new MemoryStream();
            MemoryStream walStream = new MemoryStream();

            using (AtomicStream stream = new AtomicStream(mainStream, walStream))
            {
                stream.Write(new byte[] { 1, 2, 3, 4, 5 }, 2, 2);
                stream.Commit();

                byte[] buffer = new byte[4];
                stream.Position = 0;

                Assert.That(stream.Read(buffer, 1, 3), Is.EqualTo(2));
                Assert.That(buffer, Is.EqualTo(new byte[] { 0, 3, 4, 0 }));
            }
        }
    }
}