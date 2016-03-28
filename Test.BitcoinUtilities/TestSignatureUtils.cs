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
            byte[] privateKey = new byte[]
            {
                0xC3, 0x54, 0x83, 0x30, 0xDA, 0x47, 0x8E, 0x50, 0x06, 0x49, 0xD4, 0xD5, 0x9F, 0x90, 0x69, 0x58,
                0x98, 0x79, 0x2A, 0x3D, 0xC0, 0x2E, 0xD0, 0x54, 0xEA, 0x01, 0x1B, 0x69, 0xED, 0x3E, 0x42, 0x59
            };

            for (int i = 0; i < 16; i++)
            {
                SignAndVerify("ABC", privateKey, true);
                SignAndVerify("ABC", privateKey, false);
            }
        }

        private void SignAndVerify(string message, byte[] key, bool useCompressedPublicKey)
        {
            string address = BitcoinAddress.FromPrivateKey(key, useCompressedPublicKey);
            string signature = SignatureUtils.SingMesssage(message, key, useCompressedPublicKey);
            byte[] encodedPubliKey = BitcoinPrivateKey.ToEncodedPublicKey(key, useCompressedPublicKey);

            Console.WriteLine("---- SignAndVerify ----");
            Console.WriteLine("Address: {0}", address);
            Console.WriteLine("Message: {0}", message);
            Console.WriteLine("Signature: {0}", signature);
            Assert.That(SignatureUtils.VerifyMessage(encodedPubliKey, signature, message), Is.True);
        }
    }
}