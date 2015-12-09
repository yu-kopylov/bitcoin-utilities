using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using NUnit.Framework;

namespace Test.BitcoinUtilities.P2P
{
    [TestFixture]
    public class TestBitcoinMessageFormatter
    {
        private class FakeMessage : IBitcoinMessage
        {
            public string Command
            {
                get { return "fake"; }
            }

            public void Write(BitcoinStreamWriter writer)
            {
                throw new System.NotImplementedException();
            }
        }

        [Test]
        public void Test()
        {
            BitcoinMessageFormatter formatter = new BitcoinMessageFormatter("\t");
            Assert.That(formatter.Format(null), Is.EqualTo("\t<null>"));
            Assert.That(formatter.Format(new FakeMessage()), Is.StringStarting("\tcommand:\n\t\tfake\n\t<Formatting is not supported"));
            Assert.That(
                formatter.Format(
                    new InvMessage(
                        new InventoryVector[]
                        {
                            new InventoryVector(InventoryVectorType.MsgBlock, new byte[32])
                        })
                    ),
                Is.EqualTo(
                    "\tcommand:" +
                    "\n\t\tinv" +
                    "\n\tinventory items [1 item]:" +
                    "\n\t\tMsgBlock\t" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-" +
                    "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00"
                    ));
        }
    }
}