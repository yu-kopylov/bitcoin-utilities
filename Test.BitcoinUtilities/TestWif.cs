using System;
using BitcoinUtilities;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestWif
    {
        [Test]
        public void TestCompressed()
        {
            TestEncodeDecode(BitcoinNetworkKind.Main, KnownAddresses.ReferenceExample.PrivateKey, "KwdMAjGmerYanjeui5SHS7JkmpZvVipYvB2LJGU1ZxJwYvP98617", true);
            TestEncodeDecode(BitcoinNetworkKind.Main, KnownAddresses.UncompressedX0.PrivateKey, "KwDiBf89QgGbjEhKnhXJuH7LrciVrZi3qYjgd9M7rFU8MZejxwYf", true);
            TestEncodeDecode(BitcoinNetworkKind.Main, KnownAddresses.UncompressedY0.PrivateKey, "KwDiBf89QgGbjEhKnhXJuH7LrciVrZi3qYjgd9M7rFU868E4dnmx", true);

            TestEncodeDecode(BitcoinNetworkKind.Test, KnownAddresses.ReferenceExample.PrivateKey, "cMzLdeGd5vEqxB8B6VFQoRopQ3sLAAvEzDAoQgvX54xwofSWj1fx", true);
            TestEncodeDecode(BitcoinNetworkKind.Test, KnownAddresses.UncompressedX0.PrivateKey, "cMahea7zqjxrtgAbB7LSGbcQUr1uX1ojuat9jZodMN88cJgN5CPY", true);
            TestEncodeDecode(BitcoinNetworkKind.Test, KnownAddresses.UncompressedY0.PrivateKey, "cMahea7zqjxrtgAbB7LSGbcQUr1uX1ojuat9jZodMN88LsKcKFDq", true);
        }

        [Test]
        public void TestUncompressed()
        {
            TestEncodeDecode(BitcoinNetworkKind.Main, KnownAddresses.ReferenceExample.PrivateKey, "5HueCGU8rMjxEXxiPuD5BDku4MkFqeZyd4dZ1jvhTVqvbTLvyTJ", false);
            TestEncodeDecode(BitcoinNetworkKind.Main, KnownAddresses.UncompressedX0.PrivateKey, "5HpHagT65TZzG1PH3CSu63k8DbpvD8s5ip4nEB3kEsreTsnN2UE", false);
            TestEncodeDecode(BitcoinNetworkKind.Main, KnownAddresses.UncompressedY0.PrivateKey, "5HpHagT65TZzG1PH3CSu63k8DbpvD8s5ip4nEB3kEsreQTdvJta", false);

            TestEncodeDecode(BitcoinNetworkKind.Test, KnownAddresses.ReferenceExample.PrivateKey, "91gGn1HgSap6CbU12F6z3pJri26xzp7Ay1VW6NHCoEayNXwRpu2", false);
            TestEncodeDecode(BitcoinNetworkKind.Test, KnownAddresses.UncompressedX0.PrivateKey, "91avARGdfge8E4tZfYLoxeJ5sGBdNJQH4kvjJoQFacbhEwywzqY", false);
            TestEncodeDecode(BitcoinNetworkKind.Test, KnownAddresses.UncompressedY0.PrivateKey, "91avARGdfge8E4tZfYLoxeJ5sGBdNJQH4kvjJoQFacbhBRKa2K7", false);
        }

        private void TestEncodeDecode(BitcoinNetworkKind networkKind, byte[] privateKey, string wif, bool useCompressedPublicKey)
        {
            Assert.That(Wif.Encode(networkKind, privateKey, useCompressedPublicKey), Is.EqualTo(wif));

            Assert.That(Wif.TryDecode(networkKind, wif, out var decodedPrivateKey, out var decodedUseCompressedPublicKey), Is.True);
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