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
        public void TestSigHashType_None_AnyoneCanPay_Core()
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