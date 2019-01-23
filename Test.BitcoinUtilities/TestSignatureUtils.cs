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
            TestVerifyMethod(SignatureUtils.Verify);
        }

        private void TestVerifyMethod(Func<byte[], byte[], byte[], bool> verifyMethod)
        {
            (string SignedData, string PublicKey, string Signature)[] testVectors = new[]
            {
                (
                    "11223344",
                    "02d0de0aaeaefad02b8bdc8a01a1b8b11c696bd3d66a2c5f10780d95b7df42645c",
                    "3045022100ec349d70270a0d90913dece668602925aa32b25ce08ae013f0f2fdca7c0ad9fd0220073dc832e645bae795e23daa216122c55ad617ba1a1a012e27046ff058423e48"
                ),
                (
                    "11223344",
                    "04d0de0aaeaefad02b8bdc8a01a1b8b11c696bd3d66a2c5f10780d95b7df42645cd85228a6fb29940e858e7e55842ae2bd115d1ed7cc0e82d934e929c97648cb0a",
                    "3045022100ec349d70270a0d90913dece668602925aa32b25ce08ae013f0f2fdca7c0ad9fd0220073dc832e645bae795e23daa216122c55ad617ba1a1a012e27046ff058423e48"
                ),
                (
                    "11223344",
                    "0200e3ae1974566ca06cc516d47e0fb165a674a3dabcfca15e722f0e3450f45889",
                    "304402203ed693b794f3a0fcf0362e0e2a30d519d56422c9992e172b6d178c6071320fc802205714ab66c4b95e0e121604cc5ab45f0ab06e4fd4b2796ad7d35454fa192d6531"
                ),
                (
                    "11223344",
                    "02139ae46a1133f1f9d23f25efba0f6dd87bf7ddaf568a5fb9e0a3bfda73176237",
                    "304402204a5c29e7d877eaab63554e89aa033af232466ed70873e1a47c23473a01dd9dc60220317545c6524d4531c24a9e4c019e11fa82a4823eaba51ec83911f944ccbbf41b"
                ),
                (
                    "A1B2C3",
                    "02139ae46a1133f1f9d23f25efba0f6dd87bf7ddaf568a5fb9e0a3bfda73176237",
                    "30440220101e5a219e40742f2b28e7065dadd7c8935b5d493f72eaeabc42dc579e8964e7022016b55d08aa501d31855c26fc20c6387508a23041600c09c5ca0d0a5a6b7bea92"
                )
            };

            (byte[] signedData, byte[] publicKey, byte[] signature) = RestoreVerifyTestVector(testVectors[0]);

            Assert.That(verifyMethod(signedData, publicKey, signature), Is.True);

            Assert.That(verifyMethod(null, publicKey, signature), Is.False);
            Assert.That(verifyMethod(signedData, null, signature), Is.False);
            Assert.That(verifyMethod(signedData, publicKey, null), Is.False);

            Assert.That(verifyMethod(signedData, new byte[0], signature), Is.False);
            Assert.That(verifyMethod(signedData, publicKey, new byte[0]), Is.False);

            Assert.That(verifyMethod(signedData, new byte[1], signature), Is.False);
            Assert.That(verifyMethod(signedData, publicKey, new byte[1]), Is.False);

            foreach (var testVector in testVectors)
            {
                TestVerifyMethod(verifyMethod, testVector);
            }
        }

        private void TestVerifyMethod(Func<byte[], byte[], byte[], bool> verifyMethod, (string SignedData, string PublicKey, string Signature) testVector)
        {
            (byte[] signedData, byte[] publicKey, byte[] signature) = RestoreVerifyTestVector(testVector);
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.True);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signedData[0]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            publicKey[0]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            publicKey[0]--;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            publicKey[1]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            publicKey[1]--;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            publicKey[publicKey.Length - 1]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            publicKey[publicKey.Length - 1]--;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[0]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[0]--;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[1]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[1]--;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[2]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[3]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[4]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[15]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[15]--;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[signature.Length - 1]++;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);
            signature[signature.Length - 1]--;
            Assert.That(verifyMethod(signedData, publicKey, signature), Is.False);

            (signedData, publicKey, signature) = RestoreVerifyTestVector(testVector);

            byte[] publicKey3 = new byte[3];
            byte[] publicKeyShort = new byte[publicKey.Length - 1];
            byte[] publicKeyLong = new byte[publicKey.Length + 1];

            Array.Copy(publicKey, 0, publicKey3, 0, 3);
            Array.Copy(publicKey, 0, publicKeyShort, 0, publicKey.Length - 1);
            Array.Copy(publicKey, 0, publicKeyLong, 0, publicKey.Length);

            Assert.That(verifyMethod(signedData, publicKey3, signature), Is.False);
            Assert.That(verifyMethod(signedData, publicKeyShort, signature), Is.False);
            Assert.That(verifyMethod(signedData, publicKeyLong, signature), Is.False);

            byte[] signature3 = new byte[3];
            byte[] signatureShort = new byte[signature.Length - 1];
            byte[] signatureLong = new byte[signature.Length + 1];

            Array.Copy(signature, 0, signature3, 0, 3);
            Array.Copy(signature, 0, signatureShort, 0, signature.Length - 1);
            Array.Copy(signature, 0, signatureLong, 0, signature.Length);

            Assert.That(verifyMethod(signedData, publicKey, signature3), Is.False);
            Assert.That(verifyMethod(signedData, publicKey, signatureShort), Is.False);
            Assert.That(verifyMethod(signedData, publicKey, signatureLong), Is.False);

            Assert.True(ECDSASignature.ParseDER(signature, out var parsedSignature));
            ECDSASignature swapRSSignature = new ECDSASignature(parsedSignature.S, parsedSignature.R);
            Assert.That(verifyMethod(signedData, publicKey, swapRSSignature.ToDER()), Is.False);
        }

        private (byte[], byte[], byte[]) RestoreVerifyTestVector((string SignedData, string PublicKey, string Signature) testVector)
        {
            Assert.True(HexUtils.TryGetBytes(testVector.SignedData, out byte[] signedData));
            Assert.True(HexUtils.TryGetBytes(testVector.PublicKey, out byte[] publicKey));
            Assert.True(HexUtils.TryGetBytes(testVector.Signature, out byte[] signature));
            return (signedData, publicKey, signature);
        }
    }
}