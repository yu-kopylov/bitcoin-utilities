using System;
using System.IO;
using System.Text;
using BitcoinUtilities.P2P;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace BitcoinUtilities
{
    public static class SignatureUtils
    {
        //todo: add XMLDOC
        public static bool VerifyMessage(byte[] encodedPublicKey, string signatureBase64, string message)
        {
            //todo: use Secp256K1Curve class
            X9ECParameters curveParameters = SecNamedCurves.GetByName("secp256k1");
            ECDomainParameters domainParameters = new ECDomainParameters(curveParameters.Curve, curveParameters.G, curveParameters.N, curveParameters.H);

            //todo: handle exceptions from DecodePoint
            ECPoint publicKeyPoint = curveParameters.Curve.DecodePoint(encodedPublicKey);
            ECPublicKeyParameters parameters = new ECPublicKeyParameters(publicKeyPoint, domainParameters);

            ECDsaSigner signer = new ECDsaSigner();
            signer.Init(false, parameters);

            Signature signatureParameters = ParseSignature(signatureBase64);

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

            byte[] messageHash = CryptoUtils.DoubleSha256(mem.ToArray());

            return signer.VerifySignature(messageHash, signatureParameters.R, signatureParameters.S);
        }

        private static Signature ParseSignature(string signatureBase64)
        {
            //todo: handle errors
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

            //todo: validate length

            //todo: remember and validate header agiainst recovery algorithm
            byte header = signatureBytes[0];

            var signature = new Signature();
            signature.R = new BigInteger(1, signatureBytes, 1, 32);
            signature.S = new BigInteger(1, signatureBytes, 33, 32);
            return signature;
        }

        private class Signature
        {
            public BigInteger R { get; set; }
            public BigInteger S { get; set; }
        }
    }
}