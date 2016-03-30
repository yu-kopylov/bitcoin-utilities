using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using BitcoinUtilities.P2P;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace BitcoinUtilities
{
    public static class SignatureUtils
    {
        //todo: use Secp256K1Curve class ?

        private static readonly X9ECParameters curveParameters = SecNamedCurves.GetByName("secp256k1");

        private static readonly ECDomainParameters domainParameters =
            new ECDomainParameters(curveParameters.Curve, curveParameters.G, curveParameters.N, curveParameters.H);

        private static readonly BigInteger halfCurveOrder = curveParameters.N.ShiftRight(1);

        //todo: add XMLDOC
        public static string SingMesssage(string message, byte[] privateKey, bool useCompressedPublicKey)
        {
            //todo: handle incorrect parameters

            ECPrivateKeyParameters parameters = new ECPrivateKeyParameters(new BigInteger(1, privateKey), domainParameters);

            ECDsaSigner signer = new ECDsaSigner(new HMacDsaKCalculator(new Sha256Digest()));
            signer.Init(true, parameters);

            var hash = GetMessageHash(message);

            BigInteger[] signatureArray = signer.GenerateSignature(hash);
            BigInteger r = signatureArray[0];
            BigInteger s = signatureArray[1];

            s = NormalizeS(s);

            byte[] encodedPublicKey = BitcoinPrivateKey.ToEncodedPublicKey(privateKey, useCompressedPublicKey);

            int pubKeyMaskOffset = useCompressedPublicKey ? 4 : 0;

            Signature signature = null;

            for (int i = 0; i < 4; i++)
            {
                Signature candidateSignature = new Signature();
                candidateSignature.PublicKeyMask = i + pubKeyMaskOffset;
                candidateSignature.R = r;
                candidateSignature.S = s;

                byte[] recoveredPublicKey = RecoverPublicKeyFromSignature(hash, candidateSignature);
                if (recoveredPublicKey != null && recoveredPublicKey.SequenceEqual(encodedPublicKey))
                {
                    //todo: test this
                    if (signature != null)
                    {
                        throw new Exception("ambiguous key");
                    }
                    signature = candidateSignature;
                    //break;
                }
            }

            if (signature == null)
            {
                // this should not happen
                throw new Exception("The public key is not recoverable from signature.");
            }

            return EncodeSignature(signature);
        }

        /// <summary>
        /// Checks that the given signature is valid for the given message and the given address.
        /// </summary>
        /// <param name="address">The address in Base58Check encoding.</param>
        /// <param name="message">The message.</param>
        /// <param name="signatureBase64">The signature in base-64 encoding.</param>
        /// <returns>true if the given signature is valid for the given message and the given address; otherwise, false.</returns>
        //todo: describe encoding
        public static bool VerifyMessage(string address, string message, string signatureBase64)
        {
            if (address == null || message == null || signatureBase64 == null)
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

            string recoveredAddress = BitcoinAddress.FromPublicKey(recoveredPublicKey);

            return recoveredAddress == address;
        }

        /// <summary>
        /// Normalizes the S-component of a signature according to the BIP-62.
        /// <para/>
        /// Specification: https://github.com/bitcoin/bips/blob/master/bip-0062.mediawiki (Low S values in signatures)
        /// <para/>
        /// Reference Implementation: https://github.com/bitcoin/bitcoin/blob/v0.9.3/src/key.cpp#L202-L227
        /// </summary>
        private static BigInteger NormalizeS(BigInteger s)
        {
            if (s.CompareTo(halfCurveOrder) > 0)
            {
                s = curveParameters.N.Subtract(s);
            }
            return s;
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

        // The public key recovery algorithm is described in "SEC 1: Elliptic Curve Cryptography" in section "4.1.6 Public Key Recovery Operation".
        // see: http://www.secg.org/sec1-v2.pdf
        private static byte[] RecoverPublicKeyFromSignature(byte[] hash, Signature signature)
        {
            byte encodedRPrefix = (signature.PublicKeyMask & 1) == 0 ? (byte) 2 : (byte) 3;
            BigInteger rOffset = (signature.PublicKeyMask & 2) == 0 ? BigInteger.Zero : curveParameters.N;
            bool useCompressedPublicKey = (signature.PublicKeyMask & 4) != 0;

            BigInteger e = new BigInteger(1, hash);

            BigInteger tmp = signature.R.Add(rOffset);
            BigInteger rX = tmp.Mod(Secp256K1Curve.FieldSize);
            if (tmp != rX)
            {
                //todo: test this event against reference client
                Console.WriteLine("HIT");
            }
            else
            {
                Console.WriteLine("NOT HIT");
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
            return q.GetEncoded(useCompressedPublicKey);
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