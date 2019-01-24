using System;
using BitcoinUtilities;
using NUnit.Framework;
using TestUtilities;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestSignatureUtils
    {
        [Test]
        public void TestVerifyMessage()
        {
            // WIF reference example
            string wif = "KwdMAjGmerYanjeui5SHS7JkmpZvVipYvB2LJGU1ZxJwYvP98617";
            string address = "1LoVGDgRs9hTfTNJNuXKSpywcbdvwRXpmK";
            string message = "test";
            string signature = "IKbIRnBqx8C1S6cRGcBX778Ek/0aM8z3XSxZDGIBhOcVMVw+y5ORLdvkJ7uCgWwc0D7WvRFeZfpma0BleAslDEU=";

            Assert.True(SignatureUtils.VerifyMessage(address, message, signature));

            // wrong message
            Assert.False(SignatureUtils.VerifyMessage(address, message + "X", signature));

            // wrong private key
            byte[] corruptedPrivateKey;
            bool compressed;
            Assert.True(Wif.TryDecode(BitcoinNetworkKind.Main, wif, out corruptedPrivateKey, out compressed));
            corruptedPrivateKey[17]++;
            Assert.False(SignatureUtils.VerifyMessage(BitcoinAddress.FromPrivateKey(BitcoinNetworkKind.Main, corruptedPrivateKey, compressed), message, signature));

            // wrong compression
            byte[] privateKey;
            Assert.True(Wif.TryDecode(BitcoinNetworkKind.Main, wif, out privateKey, out compressed));
            Assert.False(SignatureUtils.VerifyMessage(BitcoinAddress.FromPrivateKey(BitcoinNetworkKind.Main, privateKey, !compressed), message, signature));

            // corruprted signature header
            for (byte header = 0x1A; header <= 0x23; header++)
            {
                byte[] corruptedSignature = Convert.FromBase64String(signature);
                if (corruptedSignature[0] == header)
                {
                    continue;
                }

                corruptedSignature[0] = header;
                Assert.False(SignatureUtils.VerifyMessage(address, message, Convert.ToBase64String(corruptedSignature)));
            }

            // corruprted r-component of signature
            {
                byte[] corruptedSignature = Convert.FromBase64String(signature);
                corruptedSignature[11]++;
                Assert.False(SignatureUtils.VerifyMessage(address, message, Convert.ToBase64String(corruptedSignature)));
            }

            // corruprted s-component of signature
            {
                byte[] corruptedSignature = Convert.FromBase64String(signature);
                corruptedSignature[43]++;
                Assert.False(SignatureUtils.VerifyMessage(address, message, Convert.ToBase64String(corruptedSignature)));
            }
        }

        [Test]
        public void TestVerifyMessageValidation()
        {
            // WIF reference example
            string address = "1LoVGDgRs9hTfTNJNuXKSpywcbdvwRXpmK";
            string message = "test";
            string signature = "IKbIRnBqx8C1S6cRGcBX778Ek/0aM8z3XSxZDGIBhOcVMVw+y5ORLdvkJ7uCgWwc0D7WvRFeZfpma0BleAslDEU=";

            Assert.True(SignatureUtils.VerifyMessage(address, message, signature));

            Assert.False(SignatureUtils.VerifyMessage(null, message, signature));
            Assert.False(SignatureUtils.VerifyMessage(address, null, signature));
            Assert.False(SignatureUtils.VerifyMessage(address, message, null));

            Assert.False(SignatureUtils.VerifyMessage("", message, signature));
            Assert.False(SignatureUtils.VerifyMessage(address, "", signature));
            Assert.False(SignatureUtils.VerifyMessage(address, message, ""));

            // one byte signature
            byte[] oneByteSignature = new byte[1];
            oneByteSignature[0] = 0x1B;
            Assert.False(SignatureUtils.VerifyMessage(address, message, Convert.ToBase64String(oneByteSignature)));

            // signature contains only zeros
            byte[] zeroSignature = new byte[65];
            Assert.False(SignatureUtils.VerifyMessage(address, message, Convert.ToBase64String(zeroSignature)));

            // r and s in signature contains only zeros, header is not empty
            byte[] zeroSignatureWithHeader = new byte[65];
            zeroSignatureWithHeader[0] = 0x1B;
            Assert.False(SignatureUtils.VerifyMessage(address, message, Convert.ToBase64String(zeroSignatureWithHeader)));

            // invalid point (r = Q - 1, s = Q - 1)
            Assert.False(SignatureUtils.VerifyMessage(
                address,
                message,
                "G/////////////////////////////////////7///wu/////////////////////////////////////v///C4="));

            // signature is not a valid base-64 string
            Assert.False(SignatureUtils.VerifyMessage(address, message, signature + "*"));
        }

        [Test]
        public void TestSignMessageValidation()
        {
            // WIF reference example
            string message = "test";
            string signature = "IKbIRnBqx8C1S6cRGcBX778Ek/0aM8z3XSxZDGIBhOcVMVw+y5ORLdvkJ7uCgWwc0D7WvRFeZfpma0BleAslDEU=";

            byte[] privateKey = KnownAddresses.ReferenceExample.PrivateKey;

            Assert.That(SignatureUtils.SignMessage(message, privateKey, true), Is.EqualTo(signature));

            Assert.Throws<ArgumentException>(() => SignatureUtils.SignMessage(null, privateKey, true));
            Assert.Throws<ArgumentException>(() => SignatureUtils.SignMessage(message, null, true));

            Assert.Throws<ArgumentException>(() => SignatureUtils.SignMessage(message, new byte[0], true));
            Assert.Throws<ArgumentException>(() => SignatureUtils.SignMessage(message, new byte[32], true));
        }

        [Test]
        public void TestSignAndVerifyMessage()
        {
            // WIF reference example
            // WIF (compressed):        KwdMAjGmerYanjeui5SHS7JkmpZvVipYvB2LJGU1ZxJwYvP98617
            // address (compressed):    1LoVGDgRs9hTfTNJNuXKSpywcbdvwRXpmK
            // WIF (uncompressed):      5HueCGU8rMjxEXxiPuD5BDku4MkFqeZyd4dZ1jvhTVqvbTLvyTJ
            // address (uncompressed):  1GAehh7TsJAHuUAeKZcXf5CnwuGuGgyX2S
            byte[] privateKey = KnownAddresses.ReferenceExample.PrivateKey;

            // S-component normalization
            SignAndVerifyMessage("ABCD_0001", privateKey, true, "IL21huoowwTtYLdUSTXCClKxecI4c5VLweXzvXIVS17hXtriqRyNMYpwtg2BySlfPHP2Yw998Ml2Ac5RNOS2W1I=");

            // signature header is 0x1B
            SignAndVerifyMessage("ABCD_0002", privateKey, false, "G0n7e+6b7ye9La0HdCJYBZOyOb4xlWMm9vHVLAQA9Rp2bdRJSiAuJiUqa7KGnNKO70IeqMdqnuOWhJ9BAGGRkEk=");

            // signature header is 0x1C
            SignAndVerifyMessage("ABCD_0000", privateKey, false, "HGiWS/eo1pc0KMH54YB76h6+0PMCdm1KtN+9HTXrDzRfUNH4RpBn2WQdLBb+Ary8Qbt4U0IiERbLvuYHa1zzKbI=");

            // signature header is 0x1F
            SignAndVerifyMessage("ABCD_0002", privateKey, true, "H0n7e+6b7ye9La0HdCJYBZOyOb4xlWMm9vHVLAQA9Rp2bdRJSiAuJiUqa7KGnNKO70IeqMdqnuOWhJ9BAGGRkEk=");

            // signature header is 0x20
            SignAndVerifyMessage("ABCD_0000", privateKey, true, "IGiWS/eo1pc0KMH54YB76h6+0PMCdm1KtN+9HTXrDzRfUNH4RpBn2WQdLBb+Ary8Qbt4U0IiERbLvuYHa1zzKbI=");

            // non ASCII characters
            SignAndVerifyMessage("проверка", privateKey, true, "IIuYtuiOt6yRk8eAtXRMHW4K27Kgoxqj6uMi5kLywnhTKPJwxn3AQPZfEmViz0amIaGl5BQVmqhJYhYA1BwS+vw=");
        }

        private void SignAndVerifyMessage(string message, byte[] key, bool useCompressedPublicKey, string expectedSignature)
        {
            string address = BitcoinAddress.FromPrivateKey(BitcoinNetworkKind.Main, key, useCompressedPublicKey);
            string signature = SignatureUtils.SignMessage(message, key, useCompressedPublicKey);
            Console.WriteLine("---- SignAndVerifyMessage ----");
            Console.WriteLine("Address:\t{0}", address);
            Console.WriteLine("WIF:\t\t{0}", Wif.Encode(BitcoinNetworkKind.Main, key, useCompressedPublicKey));
            Console.WriteLine("Message:\t{0}", message);
            Console.WriteLine("Signature:\t{0}", signature);
            Console.WriteLine("Signature Header:\t0x{0:X2}", Convert.FromBase64String(signature)[0]);
            Assert.That(signature, Is.EqualTo(expectedSignature));
            Assert.True(SignatureUtils.VerifyMessage(address, message, signature));
        }

        [Test]
        public void TestVerify()
        {
            VerifyTestVector[] verifyTestVectors = new VerifyTestVector[]
            {
                new VerifyTestVector
                (
                    "ref. key (compressed)", true,
                    "AA00000001",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "30450221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b02202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc"
                ),
                new VerifyTestVector
                (
                    "ref. key (uncompressed)", true,
                    "AA00000001",
                    KnownAddresses.ReferenceExample.PublicKeyUncompressed,
                    "30450221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b02202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc"
                ),
                new VerifyTestVector
                (
                    "bip-38 key", true,
                    "AA00000001",
                    KnownAddresses.Bip38Example.PublicKeyCompressed,
                    "3045022100993531ec6216f740d4b1fa210c776c0b001e41f8ca6a43920b2f919906e7291802206db1fe0f6d6a6738b4df05cf45bd113d7c65da53a18aabde1ca620d6ae0fcd61"
                ),
                new VerifyTestVector
                (
                    "uncompressed X0", true,
                    "AA00000001",
                    KnownAddresses.UncompressedX0.PublicKeyUncompressed,
                    "30450221009398e985fa37ef79c8d5fb06a21f68f618bbe68824478ccf9115e80f909f085e022008b4d4a2ad2f2f16b7629e8b0b3bc196fe075c943fc4fbca0d8c4ebbb4044bf2"
                ),
                new VerifyTestVector
                (
                    "uncompressed Y0", true,
                    "AA00000001",
                    KnownAddresses.UncompressedY0.PublicKeyUncompressed,
                    "30440220680fcde47b352b1ade4bd06faa7c00f28a36ea6eaaa93d1e7d0b771e811765f1022047820b207f910e57c4e2731c57771c7a3bee60fd6e1e6db2d9336b3b4c237b82"
                ),
                new VerifyTestVector
                (
                    "low-point key", true,
                    "AA00000001",
                    KnownAddresses.LowPoint.PublicKeyCompressed,
                    "3045022100f2c3d0a8eedd412c760e94231571bf04d7f5e8b0eea683bc0c472f0c55e4b9b60220289bfa9f92e47a9e5b50e95634886ea9fa8a55f5ed69834c78573f25d297b7da"
                ),
                new VerifyTestVector
                (
                    "long R and S (no normalization)", true,
                    "AA00000004",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "3046022100cd46a1aa9795d12f3cd9629745d95d0e9ad053be232d0c66a7673c753ac7642c022100d2224357419d1c581f92fc3d046d46d45d3ec2bd446654aef5d67f45a0264b25"
                ),
                new VerifyTestVector
                (
                    "short R and S", true,
                    "AA001191CF",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "3042021f18e8f26bca42697da704abfaa20111f63adbda1803dbbf82b27738654bd484021f3c76fb042a8ac1b7e9d9040444d5905984b14dd32ce4eca47b250159c7f615"
                ),
                new VerifyTestVector
                (
                    "non-deterministic K", true,
                    "AA00000001",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "3045022100d26a4d8e12faadd8671ad45c6b90fd493b6509887915d6a906a03f7b156c4286022062ef5ccae1b1381f4dc78a4381e48f60ce52570b89643e0a59cd6fd4e729e74f"
                ),
                new VerifyTestVector
                (
                    "key-signature mismatch", false,
                    "AA00000001",
                    KnownAddresses.Bip38Example.PublicKeyCompressed,
                    "30450221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b02202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc"
                ),
                new VerifyTestVector
                (
                    "invalid public key encoding", false,
                    "AA00000001",
                    HexUtils.GetBytesUnsafe("09d0de0aaeaefad02b8bdc8a01a1b8b11c696bd3d66a2c5f10780d95b7df42645c"),
                    "30450221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b02202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc"
                ),
                new VerifyTestVector
                (
                    "invalid public key compression", false,
                    "AA00000001",
                    HexUtils.GetBytesUnsafe("02d0de0aaeaefad02b8bdc8a01a1b8b11c696bd3d66a2c5f10780d95b7df4264FF"),
                    "30450221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b02202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc"
                ),
                new VerifyTestVector
                (
                    "zero public key", false,
                    "AA00000001",
                    new byte[1],
                    "30450221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b02202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc"
                ),
                new VerifyTestVector
                (
                    "zero signature", false,
                    "AA00000001",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "3006020100020100"
                ),
                new VerifyTestVector
                (
                    "zero R", false,
                    "AA00000001",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "302502010002202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc"
                ),
                new VerifyTestVector
                (
                    "zero S", false,
                    "AA00000001",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "30260221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b020100"
                ),
                new VerifyTestVector
                (
                    "negative R", false,
                    "AA00000004",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "30450220cd46a1aa9795d12f3cd9629745d95d0e9ad053be232d0c66a7673c753ac7642c022100d2224357419d1c581f92fc3d046d46d45d3ec2bd446654aef5d67f45a0264b25"
                ),
                new VerifyTestVector
                (
                    "negative S", false,
                    "AA00000004",
                    KnownAddresses.ReferenceExample.PublicKeyCompressed,
                    "3045022100cd46a1aa9795d12f3cd9629745d95d0e9ad053be232d0c66a7673c753ac7642c0220d2224357419d1c581f92fc3d046d46d45d3ec2bd446654aef5d67f45a0264b25"
                )
            };

            foreach (var testVector in verifyTestVectors)
            {
                Assert.That(SignatureUtils.Verify(testVector.SignedData, testVector.PublicKey, testVector.Signature), Is.EqualTo(testVector.IsValid), testVector.Name);
            }
        }

        [Test]
        public void TestVerifyValidation()
        {
            byte[] signedData = HexUtils.GetBytesUnsafe("AA00000001");
            byte[] publicKey = KnownAddresses.ReferenceExample.PublicKeyCompressed;
            byte[] signature = HexUtils.GetBytesUnsafe("30450221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b02202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc");

            Assert.True(SignatureUtils.Verify(signedData, publicKey, signature));

            Assert.False(SignatureUtils.Verify(null, publicKey, signature));
            Assert.False(SignatureUtils.Verify(signedData, null, signature));
            Assert.False(SignatureUtils.Verify(signedData, publicKey, null));

            Assert.False(SignatureUtils.Verify(signedData, new byte[0], signature));
            Assert.False(SignatureUtils.Verify(signedData, publicKey, new byte[0]));

            Assert.False(SignatureUtils.Verify(signedData, new byte[1], signature));
            Assert.False(SignatureUtils.Verify(signedData, publicKey, new byte[1]));

            Assert.False(SignatureUtils.Verify(signedData, new byte[] {0x02}, signature));
            Assert.False(SignatureUtils.Verify(signedData, publicKey, new byte[] {0x30}));

            Assert.False(SignatureUtils.Verify(signedData, new byte[] {0x02, 0xd0}, signature));
            Assert.False(SignatureUtils.Verify(signedData, publicKey, new byte[] {0x30, 0x45}));
        }

        [Test]
        public void TestVerifyConcurrency()
        {
            byte[] signedData = HexUtils.GetBytesUnsafe("AA00000001");
            byte[] publicKey = KnownAddresses.ReferenceExample.PublicKeyCompressed;
            byte[] signature = HexUtils.GetBytesUnsafe("30450221008f4c18a6de5348db2bd3c3c9c880c18c9f68c327617f33834f4802a0ca76b63b02202e640e9cd8d503d216c6bdf725a1c377c1feedba6548e97a1e2d418a2e069fcc");

            TestUtils.TestConcurrency(10, 100, (t, i) => SignatureUtils.Verify(signedData, publicKey, signature));
        }

        private class VerifyTestVector
        {
            public VerifyTestVector(string name, bool isValid, string signedData, byte[] publicKey, string signature)
            {
                Name = name;
                IsValid = isValid;
                SignedData = HexUtils.GetBytesUnsafe(signedData);
                PublicKey = publicKey;
                Signature = HexUtils.GetBytesUnsafe(signature);
            }

            public string Name { get; }
            public bool IsValid { get; }
            public byte[] SignedData { get; }
            public byte[] PublicKey { get; }
            public byte[] Signature { get; }
        }
    }
}