﻿using System;
using System.Linq;
using System.Text;
using BitcoinUtilities;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestCryptoUtils
    {
        [Test]
        public void TestDoubleSha256()
        {
            byte[] hash = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("hello"));
            Assert.That(hash, Is.EqualTo(new byte[]
            {
                0x95, 0x95, 0xC9, 0xDF, 0x90, 0x07, 0x51, 0x48, 0xEB, 0x06, 0x86, 0x03, 0x65, 0xDF, 0x33, 0x58,
                0x4B, 0x75, 0xBF, 0xF7, 0x82, 0xA5, 0x10, 0xC6, 0xCD, 0x48, 0x83, 0xA4, 0x19, 0x83, 0x3D, 0x50
            }));
        }

        [Test]
        public void TestDoubleSha256Exceptions()
        {
            byte[] originalHash = CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("123"));

            Assert.Throws<ArgumentNullException>(() => CryptoUtils.DoubleSha256(null));
            Assert.That(CryptoUtils.DoubleSha256(Encoding.ASCII.GetBytes("123")), Is.EqualTo(originalHash));
        }

        [Test]
        public void TestSha256()
        {
            byte[] hash;

            byte[] emptyHash = new byte[]
            {
                0xE3, 0xB0, 0xC4, 0x42, 0x98, 0xFC, 0x1C, 0x14, 0x9A, 0xFB, 0xF4, 0xC8, 0x99, 0x6F, 0xB9, 0x24,
                0x27, 0xAE, 0x41, 0xE4, 0x64, 0x9B, 0x93, 0x4C, 0xA4, 0x95, 0x99, 0x1B, 0x78, 0x52, 0xB8, 0x55
            };

            hash = CryptoUtils.Sha256();
            Assert.That(hash, Is.EqualTo(emptyHash));

            hash = CryptoUtils.Sha256(new byte[0]);
            Assert.That(hash, Is.EqualTo(emptyHash));

            byte[] abcHash = new byte[]
            {
                0xBA, 0x78, 0x16, 0xBF, 0x8F, 0x01, 0xCF, 0xEA, 0x41, 0x41, 0x40, 0xDE, 0x5D, 0xAE, 0x22, 0x23,
                0xB0, 0x03, 0x61, 0xA3, 0x96, 0x17, 0x7A, 0x9C, 0xB4, 0x10, 0xFF, 0x61, 0xF2, 0x00, 0x15, 0xAD
            };

            hash = CryptoUtils.Sha256(Encoding.ASCII.GetBytes("abc"));
            Assert.That(hash, Is.EqualTo(abcHash));

            hash = CryptoUtils.Sha256(Encoding.ASCII.GetBytes("ab"), Encoding.ASCII.GetBytes("c"));
            Assert.That(hash, Is.EqualTo(abcHash));
        }

        [Test]
        public void TestSha256Exceptions()
        {
            byte[] originalHash = CryptoUtils.Sha256(Encoding.ASCII.GetBytes("123"));

            Assert.Throws<ArgumentNullException>(() => CryptoUtils.Sha256(new byte[] {1}, null, new byte[] {2}));
            Assert.That(CryptoUtils.Sha256(Encoding.ASCII.GetBytes("123")), Is.EqualTo(originalHash));

            Assert.Throws<ArgumentNullException>(() => CryptoUtils.Sha256(null));
            Assert.That(CryptoUtils.Sha256(Encoding.ASCII.GetBytes("123")), Is.EqualTo(originalHash));
        }

        [Test]
        public void TestSha256Concurrency()
        {
            byte[] shortData1 = Enumerable.Range(1, 128).Select(i => (byte) i).ToArray();
            byte[] shortData2 = Enumerable.Range(100, 128).Select(i => (byte) i).ToArray();
            byte[] longData = Enumerable.Range(1, 16384).Select(i => (byte) (i / 3)).ToArray();

            byte[] shortData1DoubleHash = CryptoUtils.DoubleSha256(shortData1);
            byte[] shortData2DoubleHash = CryptoUtils.DoubleSha256(shortData2);
            byte[] shortData12Hash = CryptoUtils.Sha256(shortData1, shortData2);
            byte[] longDataDoubleHash = CryptoUtils.DoubleSha256(longData);

            TestUtils.TestConcurrency(16, 1000, (t, i) =>
            {
                bool res = true;
                res &= CryptoUtils.DoubleSha256(shortData1).SequenceEqual(shortData1DoubleHash);
                res &= CryptoUtils.DoubleSha256(shortData2).SequenceEqual(shortData2DoubleHash);
                res &= CryptoUtils.Sha256(shortData1, shortData2).SequenceEqual(shortData12Hash);
                res &= CryptoUtils.DoubleSha256(longData).SequenceEqual(longDataDoubleHash);
                return res;
            });
        }

        [Test]
        public void TestSha512()
        {
            byte[] hash;

            byte[] emptyHash = new byte[]
            {
                0xCF, 0x83, 0xE1, 0x35, 0x7E, 0xEF, 0xB8, 0xBD, 0xF1, 0x54, 0x28, 0x50, 0xD6, 0x6D, 0x80, 0x07,
                0xD6, 0x20, 0xE4, 0x05, 0x0B, 0x57, 0x15, 0xDC, 0x83, 0xF4, 0xA9, 0x21, 0xD3, 0x6C, 0xE9, 0xCE,
                0x47, 0xD0, 0xD1, 0x3C, 0x5D, 0x85, 0xF2, 0xB0, 0xFF, 0x83, 0x18, 0xD2, 0x87, 0x7E, 0xEC, 0x2F,
                0x63, 0xB9, 0x31, 0xBD, 0x47, 0x41, 0x7A, 0x81, 0xA5, 0x38, 0x32, 0x7A, 0xF9, 0x27, 0xDA, 0x3E
            };

            hash = CryptoUtils.Sha512();
            Assert.That(hash, Is.EqualTo(emptyHash));

            hash = CryptoUtils.Sha512(new byte[0]);
            Assert.That(hash, Is.EqualTo(emptyHash));

            byte[] abcHash = new byte[]
            {
                0xDD, 0xAF, 0x35, 0xA1, 0x93, 0x61, 0x7A, 0xBA, 0xCC, 0x41, 0x73, 0x49, 0xAE, 0x20, 0x41, 0x31,
                0x12, 0xE6, 0xFA, 0x4E, 0x89, 0xA9, 0x7E, 0xA2, 0x0A, 0x9E, 0xEE, 0xE6, 0x4B, 0x55, 0xD3, 0x9A,
                0x21, 0x92, 0x99, 0x2A, 0x27, 0x4F, 0xC1, 0xA8, 0x36, 0xBA, 0x3C, 0x23, 0xA3, 0xFE, 0xEB, 0xBD,
                0x45, 0x4D, 0x44, 0x23, 0x64, 0x3C, 0xE8, 0x0E, 0x2A, 0x9A, 0xC9, 0x4F, 0xA5, 0x4C, 0xA4, 0x9F
            };

            hash = CryptoUtils.Sha512(Encoding.ASCII.GetBytes("abc"));
            Assert.That(hash, Is.EqualTo(abcHash));

            hash = CryptoUtils.Sha512(Encoding.ASCII.GetBytes("ab"), Encoding.ASCII.GetBytes("c"));
            Assert.That(hash, Is.EqualTo(abcHash));
        }

        [Test]
        public void TestSha512Exceptions()
        {
            byte[] originalHash = CryptoUtils.Sha512(Encoding.ASCII.GetBytes("123"));

            Assert.Throws<ArgumentNullException>(() => CryptoUtils.Sha512(new byte[] {1}, null, new byte[] {2}));
            Assert.That(CryptoUtils.Sha512(Encoding.ASCII.GetBytes("123")), Is.EqualTo(originalHash));

            Assert.Throws<ArgumentNullException>(() => CryptoUtils.Sha512(null));
            Assert.That(CryptoUtils.Sha512(Encoding.ASCII.GetBytes("123")), Is.EqualTo(originalHash));
        }

        [Test]
        public void TestSha1()
        {
            byte[] hash = CryptoUtils.Sha1(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"));
            Assert.That(hash, Is.EqualTo(new byte[]
            {
                0x2f, 0xd4, 0xe1, 0xc6, 0x7a, 0x2d, 0x28, 0xfc, 0xed, 0x84, 0x9e, 0xe1, 0xbb, 0x76, 0xe7, 0x39, 0x1b, 0x93, 0xeb, 0x12
            }));
        }

        [Test]
        public void TestSha1Exceptions()
        {
            byte[] originalHash = CryptoUtils.Sha1(Encoding.ASCII.GetBytes("123"));

            Assert.Throws<ArgumentNullException>(() => CryptoUtils.Sha1(null));
            Assert.That(CryptoUtils.Sha1(Encoding.ASCII.GetBytes("123")), Is.EqualTo(originalHash));
        }

        [Test]
        public void TestRipeMd160()
        {
            byte[] hash = CryptoUtils.RipeMd160(Encoding.ASCII.GetBytes("The quick brown fox jumps over the lazy dog"));
            Assert.That(hash, Is.EqualTo(new byte[]
            {
                0x37, 0xf3, 0x32, 0xf6, 0x8d, 0xb7, 0x7b, 0xd9, 0xd7, 0xed, 0xd4, 0x96, 0x95, 0x71, 0xad, 0x67, 0x1c, 0xf9, 0xdd, 0x3b
            }));
        }

        [Test]
        public void TestRipeMd160Exceptions()
        {
            byte[] originalHash = CryptoUtils.RipeMd160(Encoding.ASCII.GetBytes("123"));

            Assert.Throws<ArgumentNullException>(() => CryptoUtils.RipeMd160(null));
            Assert.That(CryptoUtils.RipeMd160(Encoding.ASCII.GetBytes("123")), Is.EqualTo(originalHash));
        }
    }
}