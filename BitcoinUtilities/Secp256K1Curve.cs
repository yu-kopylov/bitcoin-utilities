using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace BitcoinUtilities
{
    public static class Secp256K1Curve
    {
        private static readonly ECPoint g;
        private static readonly BigInteger n;
        private static readonly BigInteger fieldSize;
        private static readonly BigInteger groupSize;

        static Secp256K1Curve()
        {
            X9ECParameters parameters = SecNamedCurves.GetByName("secp256k1");
            g = parameters.G;
            n = parameters.N;
            fieldSize = ((FpCurve)parameters.Curve).Q;
            groupSize = parameters.N;
        }

        public static ECPoint G
        {
            get { return g; }
        }

        public static BigInteger N
        {
            get { return n; }
        }

        public static BigInteger FieldSize
        {
            get { return fieldSize; }
        }

        public static BigInteger GroupSize
        {
            get { return groupSize; }
        }
    }
}