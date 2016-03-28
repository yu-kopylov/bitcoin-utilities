using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using BitcoinUtilities.P2P;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace BitcoinUtilities
{
    public static class SignatureUtils
    {
        //todo: add XMLDOC
        public static string SingMesssage(string message, byte[] privateKey, bool useCompressedPublicKey)
        {
            //todo: handle incorrect parameters

            //todo: use Secp256K1Curve class
            X9ECParameters curveParameters = SecNamedCurves.GetByName("secp256k1");
            ECDomainParameters domainParameters = new ECDomainParameters(curveParameters.Curve, curveParameters.G, curveParameters.N, curveParameters.H);
            ICipherParameters parameters = new ECPrivateKeyParameters(new BigInteger(1, privateKey), domainParameters);

            //todo: review K values generator initalization in the new version of BouncyCastle 
            ECDsaSigner signer = new ECDsaSigner();

            var secureRandom = new Org.BouncyCastle.Security.SecureRandom(SecureRandomSeedGenerator.CreateSeed());
            signer.Init(true, new ParametersWithRandom(parameters, secureRandom));

            var hash = GetMessageHash(message);

            BigInteger[] signatureArray = signer.GenerateSignature(hash);

            byte[] encodedPublicKey = BitcoinPrivateKey.ToEncodedPublicKey(privateKey, useCompressedPublicKey);

            int pubKeyMaskOffset = useCompressedPublicKey ? 4 : 0;

            Signature signature = null;

            for (int i = 0; i < 4; i++)
            {
                Signature candidateSignature = new Signature();
                candidateSignature.PublicKeyMask = i + pubKeyMaskOffset;
                candidateSignature.R = signatureArray[0];
                candidateSignature.S = signatureArray[1];

                byte[] recoveredPublicKey = RecoverPublicKeyFromSignature(hash, candidateSignature);
                if (recoveredPublicKey != null && recoveredPublicKey.SequenceEqual(encodedPublicKey))
                {
                    signature = candidateSignature;
                    break;
                }
            }

            if (signature == null)
            {
                // this should not happen
                throw new Exception("The public key is not recoverable from signature.");
            }

            return EncodeSignature(signature);
        }

        //todo: add XMLDOC
        public static bool VerifyMessage(byte[] encodedPublicKey, string signatureBase64, string message)
        {
            if (encodedPublicKey == null || signatureBase64 == null || message == null)
            {
                return false;
            }

            Signature signature;
            if (!ParseSignature(signatureBase64, out signature))
            {
                return false;
            }

            var hash = GetMessageHash(message);

            byte[] recoveredPublicKey = RecoverPublicKeyFromSignature(hash, signature);

            if (recoveredPublicKey == null)
            {
                return false;
            }

            // todo: is it really not necessary to verify signature (is signature always valid for recovered public ?)
            return encodedPublicKey.SequenceEqual(recoveredPublicKey);
        }

        private static byte[] GetMessageHash(string message)
        {
            byte[] messagePrefix = Encoding.ASCII.GetBytes("Bitcoin Signed Message:\n");
            //todo: fail on unmappable characters
            byte[] messageBytes = Encoding.ASCII.GetBytes(message);

            MemoryStream mem = new MemoryStream();
            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                writer.WriteCompact((ulong) messagePrefix.Length);
                writer.Write(messagePrefix, 0, messagePrefix.Length);

                writer.WriteCompact((ulong) messageBytes.Length);
                writer.Write(messageBytes, 0, messageBytes.Length);
            }

            byte[] text = mem.ToArray();
            return CryptoUtils.DoubleSha256(text);
        }

        //todo: remove ?
        private static bool VerifySignature(ECPoint publicKeyPoint, byte[] hash, Signature signature)
        {
            //todo: use Secp256K1Curve class
            X9ECParameters curveParameters = SecNamedCurves.GetByName("secp256k1");
            ECDomainParameters domainParameters = new ECDomainParameters(curveParameters.Curve, curveParameters.G, curveParameters.N, curveParameters.H);
            ECPublicKeyParameters parameters = new ECPublicKeyParameters(publicKeyPoint, domainParameters);

            ECDsaSigner signer = new ECDsaSigner();
            signer.Init(false, parameters);

            return signer.VerifySignature(hash, signature.R, signature.S);
        }

        // The public key recovery algorithm is described in "SEC 1: Elliptic Curve Cryptography" in section "4.1.6 Public Key Recovery Operation".
        // see: http://www.secg.org/sec1-v2.pdf
        private static byte[] RecoverPublicKeyFromSignature(byte[] hash, Signature signature)
        {
            //todo: use Secp256K1Curve class
            X9ECParameters curveParameters = SecNamedCurves.GetByName("secp256k1");

            byte encodedRPrefix = (signature.PublicKeyMask & 1) == 0 ? (byte) 2 : (byte) 3;
            BigInteger rOffset = (signature.PublicKeyMask & 2) == 0 ? BigInteger.Zero : curveParameters.N;
            bool useCompressedPublicKey = (signature.PublicKeyMask & 4) != 0;

            BigInteger e = new BigInteger(1, hash);

            BigInteger tmp = signature.R.Add(rOffset);
            BigInteger rX = tmp.Mod(Secp256K1Curve.FieldSize);
            if (tmp != rX)
            {
                //todo: test this event
                Console.WriteLine("HIT");
            }
            byte[] rXBytes = rX.ToByteArrayUnsigned();
            byte[] rPointEncoded = new byte[33];
            rPointEncoded[0] = encodedRPrefix;
            Array.Copy(rXBytes, 0, rPointEncoded, 33 - rXBytes.Length, rXBytes.Length);

            ECPoint rPoint;
            try
            {
                rPoint = curveParameters.Curve.DecodePoint(rPointEncoded);
            }
            catch (ArgumentException)
            {
                return null;
            }
            if (!rPoint.Multiply(curveParameters.N).IsInfinity)
            {
                return null;
            }

            ECPoint q = rPoint.Multiply(signature.S).Subtract(curveParameters.G.Multiply(e)).Multiply(signature.R.ModInverse(curveParameters.N));
            q = curveParameters.Curve.CreatePoint(q.X.ToBigInteger(), q.Y.ToBigInteger(), useCompressedPublicKey);
            return q.GetEncoded();
        }

        private static string EncodeSignature(Signature signature)
        {
            byte[] r = signature.R.ToByteArrayUnsigned();
            byte[] s = signature.S.ToByteArrayUnsigned();

            Contract.Assert(r.Length <= 32);
            Contract.Assert(s.Length <= 32);

            byte[] signatureBytes = new byte[65];
            signatureBytes[0] = (byte) (0x1B + signature.PublicKeyMask);
            Array.Copy(r, 0, signatureBytes, 33 - r.Length, r.Length);
            Array.Copy(s, 0, signatureBytes, 65 - s.Length, s.Length);

            return Convert.ToBase64String(signatureBytes);
        }

        private static bool ParseSignature(string signatureBase64, out Signature signature)
        {
            signature = null;

            if (signatureBase64 == null)
            {
                return false;
            }

            byte[] signatureBytes;
            try
            {
                signatureBytes = Convert.FromBase64String(signatureBase64);
            }
            catch (FormatException)
            {
                return false;
            }

            if (signatureBytes.Length != 65)
            {
                return false;
            }

            byte header = signatureBytes[0];

            //todo: write test
            if (header < 0x1B || header > 0x22)
            {
                return false;
            }

            signature = new Signature();
            signature.PublicKeyMask = header - 0x1B;
            signature.R = new BigInteger(1, signatureBytes, 1, 32);
            signature.S = new BigInteger(1, signatureBytes, 33, 32);

            return true;
        }

        private class Signature
        {
            public int PublicKeyMask { get; set; }
            public BigInteger R { get; set; }
            public BigInteger S { get; set; }
        }
    }
}