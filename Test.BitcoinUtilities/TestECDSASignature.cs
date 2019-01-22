using System;
using BitcoinUtilities;
using NUnit.Framework;
using Org.BouncyCastle.Math;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestECDSASignature
    {
        [Test]
        public void TestToDER()
        {
            Assert.That(
                HexUtils.GetString(new ECDSASignature(new BigInteger("0", 16), new BigInteger("0", 16)).ToDER()).ToUpperInvariant(),
                Is.EqualTo("3006" + "020100" + "020100")
            );

            Assert.That(
                HexUtils.GetString(new ECDSASignature(new BigInteger("01", 16), new BigInteger("02", 16)).ToDER()).ToUpperInvariant(),
                Is.EqualTo("3006" + "020101" + "020102")
            );

            Assert.That(
                HexUtils.GetString(new ECDSASignature(new BigInteger("-01", 16), new BigInteger("-02", 16)).ToDER()).ToUpperInvariant(),
                Is.EqualTo("3006" + "0201FF" + "0201FE")
            );

            Assert.That(
                HexUtils.GetString(new ECDSASignature(new BigInteger("7AAA", 16), new BigInteger("7BBB", 16)).ToDER()).ToUpperInvariant(),
                Is.EqualTo("3008" + "02027AAA" + "02027BBB")
            );

            Assert.That(
                HexUtils.GetString(new ECDSASignature(new BigInteger("1122334455667788", 16), new BigInteger("11", 16)).ToDER()).ToUpperInvariant(),
                Is.EqualTo("300D" + "02081122334455667788" + "020111")
            );

            Assert.That(
                HexUtils.GetString(new ECDSASignature(new BigInteger("80", 16), new BigInteger("81", 16)).ToDER()).ToUpperInvariant(),
                Is.EqualTo("3008" + "02020080" + "02020081")
            );

            Assert.That(
                HexUtils.GetString(new ECDSASignature(new BigInteger("800", 16), new BigInteger("810", 16)).ToDER()).ToUpperInvariant(),
                Is.EqualTo("3008" + "02020800" + "02020810")
            );

            Assert.That(
                HexUtils.GetString(new ECDSASignature(new BigInteger("8000", 16), new BigInteger("8100", 16)).ToDER()).ToUpperInvariant(),
                Is.EqualTo("300A" + "0203008000" + "0203008100")
            );

            Assert.That(
                HexUtils.GetString(new ECDSASignature(BigInteger.One, BigInteger.Two.Pow(121 * 8 + 6)).ToDER()).ToUpperInvariant(),
                Is.EqualTo(
                    "307F" +
                    "020101027A400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000" +
                    "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"
                )
            );

            Assert.Throws<InvalidOperationException>(() => new ECDSASignature(BigInteger.One, BigInteger.Two.Pow(121 * 8 + 7)).ToDER());
        }

        [Test]
        public void TestParseDER()
        {
            Assert.True(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x00, 0x02, 0x01, 0x00}, out var signature));
            Assert.That(signature.R, Is.EqualTo(new BigInteger("0", 16)));
            Assert.That(signature.S, Is.EqualTo(new BigInteger("0", 16)));

            Assert.True(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature.R, Is.EqualTo(new BigInteger("7F", 16)));
            Assert.That(signature.S, Is.EqualTo(new BigInteger("7E", 16)));

            Assert.True(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0xFF, 0x02, 0x01, 0xFE}, out signature));
            Assert.That(signature.R, Is.EqualTo(new BigInteger("-1", 16)));
            Assert.That(signature.S, Is.EqualTo(new BigInteger("-2", 16)));

            Assert.True(ECDSASignature.ParseDER(new byte[] {0x30, 0x08, 0x02, 0x02, 0x00, 0xFF, 0x02, 0x02, 0x00, 0xFE}, out signature));
            Assert.That(signature.R, Is.EqualTo(new BigInteger("FF", 16)));
            Assert.That(signature.S, Is.EqualTo(new BigInteger("FE", 16)));

            // non-standard (extra leading zeroes) 
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x08, 0x02, 0x02, 0x00, 0x7F, 0x02, 0x02, 0x00, 0x80}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x08, 0x02, 0x02, 0x00, 0x80, 0x02, 0x02, 0x00, 0x7F}, out signature));
            Assert.That(signature, Is.Null);

            // non-standard (extra leading ones) 
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x08, 0x02, 0x02, 0xFF, 0x80, 0x02, 0x02, 0xFF, 0x7F}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x08, 0x02, 0x02, 0xFF, 0x7F, 0x02, 0x02, 0xFF, 0x80}, out signature));
            Assert.That(signature, Is.Null);

            // null and empty
            Assert.False(ECDSASignature.ParseDER(null, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[0], out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[1], out signature));
            Assert.That(signature, Is.Null);

            // 127-byte SEQUENCE
            {
                byte[] encodedSignature = new byte[129];
                encodedSignature[0] = 0x30;
                encodedSignature[1] = 127;
                encodedSignature[2] = 0x02;
                encodedSignature[3] = 0x01;
                encodedSignature[4] = 0x01;
                encodedSignature[5] = 0x02;
                encodedSignature[6] = 122;
                encodedSignature[7] = 0x01;
                encodedSignature[128] = 0x01;
                Assert.True(ECDSASignature.ParseDER(encodedSignature, out signature));
                Assert.That(signature.R, Is.EqualTo(new BigInteger("1", 16)));
                Assert.That(signature.S, Is.EqualTo(BigInteger.Two.Pow(121 * 8).Add(BigInteger.One)));
            }

            // 128-byte SEQUENCE
            {
                byte[] encodedSignature = new byte[130];
                encodedSignature[0] = 0x30;
                encodedSignature[1] = 128;
                encodedSignature[2] = 0x02;
                encodedSignature[3] = 0x01;
                encodedSignature[4] = 0x01;
                encodedSignature[5] = 0x02;
                encodedSignature[6] = 123;
                encodedSignature[7] = 0x01;
                encodedSignature[129] = 0x01;
                Assert.False(ECDSASignature.ParseDER(encodedSignature, out signature));
                Assert.That(signature, Is.Null);
            }

            // zero length R
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x05, 0x02, 0x00, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            // zero length S
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x05, 0x02, 0x01, 0x7F, 0x02, 0x00}, out signature));
            Assert.That(signature, Is.Null);

            // incorrect SEQUENCE header
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x29, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x31, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            // incorrect SEQUENCE length
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x05, 0x02, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x07, 0x02, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x7F, 0x02, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            // incorrect INTEGER header (first)
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x01, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x03, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            // incorrect INTEGER header (second)
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x01, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x03, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            // incorrect INTEGER length (first)
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x00, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x02, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x7F, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            // incorrect INTEGER length (second)
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x00, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x02, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x7F, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            // extra bytes
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x01, 0x7E, 0x00}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x00, 0x30, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x01, 0x7E}, out signature));
            Assert.That(signature, Is.Null);

            // missing tail
            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x02, 0x01}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F, 0x02}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01, 0x7F}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02, 0x01}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06, 0x02}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30, 0x06}, out signature));
            Assert.That(signature, Is.Null);

            Assert.False(ECDSASignature.ParseDER(new byte[] {0x30}, out signature));
            Assert.That(signature, Is.Null);
        }
    }
}