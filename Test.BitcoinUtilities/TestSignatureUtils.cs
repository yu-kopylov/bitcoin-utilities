using System;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestSignatureUtils
    {
        [Test]
        public void TestVerifyMessageWithReferenceExample()
        {
            // reference example from https://bitcoin.org/en/developer-reference#verifymessage
            // const string address = "mgnucj8nYqdrPFh2JfZSB1NmUThUGnmsqe";
            // const string privateKeyWif = "cU8Q2jGeX3GNKNa5etiC8mgEgFSeVUTRQfWE2ZCzszyqYNK4Mepy";
            const bool compressed = true;
            byte[] privateKey = new byte[]
            {
                0xC3, 0x54, 0x83, 0x30, 0xDA, 0x47, 0x8E, 0x50, 0x06, 0x49, 0xD4, 0xD5, 0x9F, 0x90, 0x69, 0x58,
                0x98, 0x79, 0x2A, 0x3D, 0xC0, 0x2E, 0xD0, 0x54, 0xEA, 0x01, 0x1B, 0x69, 0xED, 0x3E, 0x42, 0x59
            };
            byte[] encodedPublicKey = BitcoinPrivateKey.ToEncodedPublicKey(privateKey, compressed);

            const string signatureBase64 = "IL98ziCmwYi5pL+dqKp4Ux+zCa4hP/xbjHmWh+Mk/lefV/0pWV1p/gQ94jgExSmgH2/+PDcCCrOHAady2IEySSI=";
            const string message = "Hello, World!";

            Assert.That(SignatureUtils.VerifyMessage(encodedPublicKey, signatureBase64, message), Is.True);

            string corruptedMessage = message + "X";
            Assert.That(SignatureUtils.VerifyMessage(encodedPublicKey, signatureBase64, corruptedMessage), Is.False);

            byte[] corruptedPrivateKey = new byte[privateKey.Length];
            Array.Copy(privateKey, corruptedPrivateKey, privateKey.Length);
            corruptedPrivateKey[17]++;
            byte[] corruptedEncodedPublicKey = BitcoinPrivateKey.ToEncodedPublicKey(corruptedPrivateKey, compressed);
            Assert.That(SignatureUtils.VerifyMessage(corruptedEncodedPublicKey, signatureBase64, message), Is.False);

            byte[] uncompressedEncodedPublicKey = BitcoinPrivateKey.ToEncodedPublicKey(privateKey, !compressed);
            Assert.That(SignatureUtils.VerifyMessage(uncompressedEncodedPublicKey, signatureBase64, message), Is.False);

            byte[] corruptedSignature = Convert.FromBase64String(signatureBase64);
            corruptedSignature[17]++;
            string corruptedSignatureBase64 = Convert.ToBase64String(corruptedSignature);
            Assert.That(SignatureUtils.VerifyMessage(encodedPublicKey, corruptedSignatureBase64, message), Is.False);
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

//            for (int i = 0; i < 64; i++)
//            {
//                SignAndVerify(string.Format("ABCD_{0:X4}", i), privateKey, true, null);
//                SignAndVerify(string.Format("ABCD_{0:X4}", i), privateKey, false, null);
//            }


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

            byte[] encodedPubliKey = BitcoinPrivateKey.ToEncodedPublicKey(key, useCompressedPublicKey);

            Console.WriteLine("---- SignAndVerify ----");
            Console.WriteLine("Address:\t{0}", address);
            Console.WriteLine("WIF:\t\t{0}", Wif.Encode(key, useCompressedPublicKey));
            Console.WriteLine("Message:\t{0}", message);
            Console.WriteLine("Signature:\t{0}", signature);
            Console.WriteLine("Signature Header:\t0x{0:X2}", Convert.FromBase64String(signature)[0]);

            Assert.That(SignatureUtils.VerifyMessage(encodedPubliKey, signature, message), Is.True);
        }
    }
}