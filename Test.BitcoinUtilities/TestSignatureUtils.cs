using System;
using BitcoinUtilities;
using NUnit.Framework;

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
            Assert.True(Wif.Decode(wif, out corruptedPrivateKey, out compressed));
            corruptedPrivateKey[17]++;
            Assert.False(SignatureUtils.VerifyMessage(BitcoinAddress.FromPrivateKey(corruptedPrivateKey, compressed), message, signature));

            // wrong compression
            byte[] privateKey;
            Assert.True(Wif.Decode(wif, out privateKey, out compressed));
            Assert.False(SignatureUtils.VerifyMessage(BitcoinAddress.FromPrivateKey(privateKey, !compressed), message, signature));

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
        public void TestSignAndVerify()
        {
            // WIF reference example
            // WIF (compressed):        KwdMAjGmerYanjeui5SHS7JkmpZvVipYvB2LJGU1ZxJwYvP98617
            // address (compressed):    1LoVGDgRs9hTfTNJNuXKSpywcbdvwRXpmK
            // WIF (uncompressed):      5HueCGU8rMjxEXxiPuD5BDku4MkFqeZyd4dZ1jvhTVqvbTLvyTJ
            // address (uncompressed):  1GAehh7TsJAHuUAeKZcXf5CnwuGuGgyX2S
            byte[] privateKey = new byte[]
            {
                0x0C, 0x28, 0xFC, 0xA3, 0x86, 0xC7, 0xA2, 0x27, 0x60, 0x0B, 0x2F, 0xE5, 0x0B, 0x7C, 0xAE, 0x11,
                0xEC, 0x86, 0xD3, 0xBF, 0x1F, 0xBE, 0x47, 0x1B, 0xE8, 0x98, 0x27, 0xE1, 0x9D, 0x72, 0xAA, 0x1D
            };

            // S-component normalization
            SignAndVerify("ABCD_0001", privateKey, true, "IL21huoowwTtYLdUSTXCClKxecI4c5VLweXzvXIVS17hXtriqRyNMYpwtg2BySlfPHP2Yw998Ml2Ac5RNOS2W1I=");

            //todo: research why headers 0x1D, 0x1E, 0x21, 0x22 does not occure

            // signature header is 0x1B
            SignAndVerify("ABCD_0002", privateKey, false, "G0n7e+6b7ye9La0HdCJYBZOyOb4xlWMm9vHVLAQA9Rp2bdRJSiAuJiUqa7KGnNKO70IeqMdqnuOWhJ9BAGGRkEk=");
            // signature header is 0x1C
            SignAndVerify("ABCD_0000", privateKey, false, "HGiWS/eo1pc0KMH54YB76h6+0PMCdm1KtN+9HTXrDzRfUNH4RpBn2WQdLBb+Ary8Qbt4U0IiERbLvuYHa1zzKbI=");
            // signature header is 0x1F
            SignAndVerify("ABCD_0002", privateKey, true, "H0n7e+6b7ye9La0HdCJYBZOyOb4xlWMm9vHVLAQA9Rp2bdRJSiAuJiUqa7KGnNKO70IeqMdqnuOWhJ9BAGGRkEk=");
            // signature header is 0x20
            SignAndVerify("ABCD_0000", privateKey, true, "IGiWS/eo1pc0KMH54YB76h6+0PMCdm1KtN+9HTXrDzRfUNH4RpBn2WQdLBb+Ary8Qbt4U0IiERbLvuYHa1zzKbI=");
        }

        private void SignAndVerify(string message, byte[] key, bool useCompressedPublicKey, string expectedSignature)
        {
            string address = BitcoinAddress.FromPrivateKey(key, useCompressedPublicKey);

            string signature = SignatureUtils.SingMesssage(message, key, useCompressedPublicKey);

            if (expectedSignature != null)
            {
                Assert.That(signature, Is.EqualTo(expectedSignature));
            }

            Console.WriteLine("---- SignAndVerify ----");
            Console.WriteLine("Address:\t{0}", address);
            Console.WriteLine("WIF:\t\t{0}", Wif.Encode(key, useCompressedPublicKey));
            Console.WriteLine("Message:\t{0}", message);
            Console.WriteLine("Signature:\t{0}", signature);
            Console.WriteLine("Signature Header:\t0x{0:X2}", Convert.FromBase64String(signature)[0]);

            Assert.True(SignatureUtils.VerifyMessage(address, message, signature));
        }
    }
}