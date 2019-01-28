using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public class TestBitcoinCoreSigHashCalculator
    {
        [Test]
        public void Test_All_AnyoneCanPay()
        {
            // Block: 300608
            // Tx Hash: cb31a26bc2b532780d5616a3729eba511ee5ac229675a3e67d2563449ab18ddc

            byte[] txText = HexUtils.GetBytesUnsafe(
                "01000000039fa40e205a5cecad73122c9352a1444065ef4fa42b954349ee1bd51f74c71da7020000008b48304502201a8747991cad3719123cc53b895d68af66" +
                "6e9f92a7425395fb898858a9ee9e96022100eeaac1102466e5662bf9096207c82bd94fbae2851787cbe7de1c33bd88a927e0814104d64438ef7a2495178af6a3" +
                "6dbe28745c24592de45d836b39c60f97dfa0a65cf416b2356e516aa94b75afd5f47aee1615d0f50a26aaf8d4d8ff8c10d0d8bc6948ffffffff9fa40e205a5cec" +
                "ad73122c9352a1444065ef4fa42b954349ee1bd51f74c71da7010000008a47304402206007b13be1802c30901d129ef6cdfc569d897e611c18b9494f3a09f0a7" +
                "a4c12f02205371050611b4c7ffd31611affe56062c4397f9e965416d3a29b36b0de0bcd40c814104228fcd8eeb4a76e6274b7fcb7de9972e71fb7b2c90fe9420" +
                "29b6cf63252e893372653e424daccdf72fbd0a69dda0f6f5dfeaec2eb81212d369ef4661b97cb58bffffffff9fa40e205a5cecad73122c9352a1444065ef4fa4" +
                "2b954349ee1bd51f74c71da7000000008c493046022100c1fe963bb931fd073206ca0791246ad5e0caae25daa8876645daf540cf958f15022100956f679a6704" +
                "0a44a1dc6f6ecc8ab9d0e23b23734ce5ffd31bdcb783bbd66c8a814104245ed9389a3e886be0ac547f83bb9ac6f92657af877eea955aefdf2e2bea09e7d19da3" +
                "67e3feaace826f3e42f0be045d66bb35394b9fcc66255a1ce3877ad7f0ffffffff03809698000000000017a914963e46b1eea894fe855d543d030f82fd4661e6" +
                "0587d8dde400000000001976a91461222f04b1ff4f44deb991c837d0aaa75890fa3788acd8dde4000000000017a9145638b284408458f728e0acc56590ff6408" +
                "b0199f8700000000"
            );

            Tx tx = BitcoinStreamReader.FromBytes(txText, Tx.Read);

            ISigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(tx);
            sigHashCalculator.InputIndex = 0;
            sigHashCalculator.Amount = 20000000;

            ScriptProcessor scriptProcessor = new ScriptProcessor();
            scriptProcessor.SigHashCalculator = sigHashCalculator;

            scriptProcessor.Execute(tx.Inputs[0].SignatureScript);
            scriptProcessor.Execute(HexUtils.GetBytesUnsafe("76a91409530cd80b6b25068154319de7aa3ef19314d3d188ac"));

            Assert.That(scriptProcessor.Valid);
            Assert.True(scriptProcessor.Success);
        }

        [Test]
        public void Test_None_AnyoneCanPay()
        {
            // Block: 299255
            // Tx Hash: 2fab5e87b7c82a3ba87725011a55903b9a274811823a68c07eae4fefdfe48130

            byte[] txText = HexUtils.GetBytesUnsafe(
                "0100000001a136731e1a17b46f423bcfac3f7760aad7cd993b4b29ba9285892ad33cf4df66010000006b48304502202a1e12d52ed31e5b80cc8bbd08d3c1e41a" +
                "f12b5fe5cca88f5f304d6a7606a218022100e936e7066eed52fff4ab6aa9c9e6a1f5d61509d5e0fe0aa9e8f32663badc1fb1822103a34b99f22c790c4e36b2b3" +
                "c2c35a36db06226e41c692fc82b8b56ac1c540c5bdffffffff010000000000000000016a00000000"
            );

            Tx tx = BitcoinStreamReader.FromBytes(txText, Tx.Read);

            ISigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(tx);
            sigHashCalculator.InputIndex = 0;
            sigHashCalculator.Amount = 10000;

            ScriptProcessor scriptProcessor = new ScriptProcessor();
            scriptProcessor.SigHashCalculator = sigHashCalculator;

            scriptProcessor.Execute(tx.Inputs[0].SignatureScript);
            scriptProcessor.Execute(HexUtils.GetBytesUnsafe("76a9149a1c78a507689f6f54b847ad1cef1e614ee23f1e88ac"));

            Assert.That(scriptProcessor.Valid);
            Assert.True(scriptProcessor.Success);
        }
    }
}