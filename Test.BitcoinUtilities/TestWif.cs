using System;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestWif
    {
        // reference example
        private readonly byte[] rfPrivateKey = new byte[]
        {
            0x0C, 0x28, 0xFC, 0xA3, 0x86, 0xC7, 0xA2, 0x27, 0x60, 0x0B, 0x2F, 0xE5, 0x0B, 0x7C, 0xAE, 0x11,
            0xEC, 0x86, 0xD3, 0xBF, 0x1F, 0xBE, 0x47, 0x1B, 0xE8, 0x98, 0x27, 0xE1, 0x9D, 0x72, 0xAA, 0x1D
        };

        // X starts with 0 
        private readonly byte[] x0PrivateKey = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x99
        };

        // Y starts with 0 
        private readonly byte[] y0PrivateKey = new byte[]
        {
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7A
        };

        [Test]
        public void TestCompressed()
        {
            // reference example
            TestEncodeDecode(BitcoinNetworkKind.Main, rfPrivateKey, "KwdMAjGmerYanjeui5SHS7JkmpZvVipYvB2LJGU1ZxJwYvP98617", true);

            // X starts with 0 
            TestEncodeDecode(BitcoinNetworkKind.Main, x0PrivateKey, "KwDiBf89QgGbjEhKnhXJuH7LrciVrZi3qYjgd9M7rFU8MZejxwYf", true);

            // Y starts with 0 
            TestEncodeDecode(BitcoinNetworkKind.Main, y0PrivateKey, "KwDiBf89QgGbjEhKnhXJuH7LrciVrZi3qYjgd9M7rFU868E4dnmx", true);

            // reference example
            TestEncodeDecode(BitcoinNetworkKind.Test, rfPrivateKey, "cMzLdeGd5vEqxB8B6VFQoRopQ3sLAAvEzDAoQgvX54xwofSWj1fx", true);

            // X starts with 0 
            TestEncodeDecode(BitcoinNetworkKind.Test, x0PrivateKey, "cMahea7zqjxrtgAbB7LSGbcQUr1uX1ojuat9jZodMN88cJgN5CPY", true);

            // Y starts with 0 
            TestEncodeDecode(BitcoinNetworkKind.Test, y0PrivateKey, "cMahea7zqjxrtgAbB7LSGbcQUr1uX1ojuat9jZodMN88LsKcKFDq", true);
        }

        [Test]
        public void TestUncompressed()
        {
            // reference example
            TestEncodeDecode(BitcoinNetworkKind.Main, rfPrivateKey, "5HueCGU8rMjxEXxiPuD5BDku4MkFqeZyd4dZ1jvhTVqvbTLvyTJ", false);

            // X starts with 0 
            TestEncodeDecode(BitcoinNetworkKind.Main, x0PrivateKey, "5HpHagT65TZzG1PH3CSu63k8DbpvD8s5ip4nEB3kEsreTsnN2UE", false);

            // Y starts with 0 
            TestEncodeDecode(BitcoinNetworkKind.Main, y0PrivateKey, "5HpHagT65TZzG1PH3CSu63k8DbpvD8s5ip4nEB3kEsreQTdvJta", false);

            // reference example
            TestEncodeDecode(BitcoinNetworkKind.Test, rfPrivateKey, "91gGn1HgSap6CbU12F6z3pJri26xzp7Ay1VW6NHCoEayNXwRpu2", false);

            // X starts with 0 
            TestEncodeDecode(BitcoinNetworkKind.Test, x0PrivateKey, "91avARGdfge8E4tZfYLoxeJ5sGBdNJQH4kvjJoQFacbhEwywzqY", false);

            // Y starts with 0 
            TestEncodeDecode(BitcoinNetworkKind.Test, y0PrivateKey, "91avARGdfge8E4tZfYLoxeJ5sGBdNJQH4kvjJoQFacbhBRKa2K7", false);
        }

        private void TestEncodeDecode(BitcoinNetworkKind networkKind, byte[] privateKey, string wif, bool useCompressedPublicKey)
        {
            Assert.That(Wif.Encode(networkKind, privateKey, useCompressedPublicKey), Is.EqualTo(wif));

            byte[] decodedPrivateKey;
            bool decodedUseCompressedPublicKey;
            Assert.That(Wif.TryDecode(networkKind, wif, out decodedPrivateKey, out decodedUseCompressedPublicKey), Is.True);
            Assert.That(decodedPrivateKey, Is.EqualTo(privateKey));
            Assert.That(decodedUseCompressedPublicKey, Is.EqualTo(useCompressedPublicKey));
        }

        [Test]
        public void TestValidation()
        {
            Assert.Throws<ArgumentException>(() => Wif.Encode(BitcoinNetworkKind.Main, null, false));
            Assert.Throws<ArgumentException>(() => Wif.Encode(BitcoinNetworkKind.Main, new byte[] {1}, false));
            Assert.Throws<ArgumentException>(() => Wif.Encode(BitcoinNetworkKind.Main, new byte[]
            {
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01
            }, false));

            byte[] decodedPrivateKey;
            bool decodedCompressionFlag;
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Main, null, out decodedPrivateKey, out decodedCompressionFlag), Is.False);
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Main, "", out decodedPrivateKey, out decodedCompressionFlag), Is.False);

            // wrong length
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Main, "2Sc7S1wac65gQ8sGKeJabXpqNCpxK7masshEtBDGDWT8Bmso7y5SN3", out decodedPrivateKey, out decodedCompressionFlag), Is.False);
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Main, "Dn3NqWBMwpyhDJgm2CCSiVzHVXrqZVC1m8GXaizFPtGBgLnJ", out decodedPrivateKey, out decodedCompressionFlag), Is.False);

            // icorrect compressed public key flag
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Main, "KwdMAjGmerYanjeui5SHS7JkmpZvVipYvB2LJGU1ZxJwZAkp2nuB", out decodedPrivateKey, out decodedCompressionFlag), Is.False);

            // incorrect address version
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Main, "5ZTbc84Pfm9mK6cMeYB4onZ6PGg13HJyvBCQxwRsQju2YRkyoe2", out decodedPrivateKey, out decodedCompressionFlag), Is.False);
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Test, "5ZTbc84Pfm9mK6cMeYB4onZ6PGg13HJyvBCQxwRsQju2YRkyoe2", out decodedPrivateKey, out decodedCompressionFlag), Is.False);

            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Main, "91gGn1HgSap6CbU12F6z3pJri26xzp7Ay1VW6NHCoEayNXwRpu2", out decodedPrivateKey, out decodedCompressionFlag), Is.False);
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Test, "91gGn1HgSap6CbU12F6z3pJri26xzp7Ay1VW6NHCoEayNXwRpu2", out decodedPrivateKey, out decodedCompressionFlag), Is.True);

            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Main, "5HueCGU8rMjxEXxiPuD5BDku4MkFqeZyd4dZ1jvhTVqvbTLvyTJ", out decodedPrivateKey, out decodedCompressionFlag), Is.True);
            Assert.That(Wif.TryDecode(BitcoinNetworkKind.Test, "5HueCGU8rMjxEXxiPuD5BDku4MkFqeZyd4dZ1jvhTVqvbTLvyTJ", out decodedPrivateKey, out decodedCompressionFlag), Is.False);
        }
    }
}