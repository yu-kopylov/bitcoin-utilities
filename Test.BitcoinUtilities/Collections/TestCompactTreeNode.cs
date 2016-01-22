using BitcoinUtilities.Collections;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Collections
{
    [TestFixture]
    public class TestCompactTreeNode
    {
        [Test]
        public void TestDataNode()
        {
            CompactTreeNode node = CompactTreeNode.CreateDataNode(0x7181818181818181);

            Assert.That(node.IsDataNode, Is.True);
            Assert.That(node.IsSplitNode, Is.False);
            Assert.That(node.Value, Is.EqualTo(0x7181818181818181));

            node = new CompactTreeNode(node.RawData);

            Assert.That(node.IsDataNode, Is.True);
            Assert.That(node.IsSplitNode, Is.False);
            Assert.That(node.Value, Is.EqualTo(0x7181818181818181));
        }

        [Test]
        public void TestSplitNode()
        {
            CompactTreeNode node = CompactTreeNode.CreateSplitNode();

            Assert.That(node.IsDataNode, Is.False);
            Assert.That(node.IsSplitNode, Is.True);
            Assert.That(node.GetChild(0), Is.EqualTo(0));
            Assert.That(node.GetChild(1), Is.EqualTo(0));

            node = new CompactTreeNode(node.RawData);

            Assert.That(node.IsDataNode, Is.False);
            Assert.That(node.IsSplitNode, Is.True);
            Assert.That(node.GetChild(0), Is.EqualTo(0));
            Assert.That(node.GetChild(1), Is.EqualTo(0));

            node = node.SetChild(0, 0x71818181);

            Assert.That(node.IsDataNode, Is.False);
            Assert.That(node.IsSplitNode, Is.True);
            Assert.That(node.GetChild(0), Is.EqualTo(0x71818181));
            Assert.That(node.GetChild(1), Is.EqualTo(0));

            node = node.SetChild(1, 0x72828282);

            Assert.That(node.IsDataNode, Is.False);
            Assert.That(node.IsSplitNode, Is.True);
            Assert.That(node.GetChild(0), Is.EqualTo(0x71818181));
            Assert.That(node.GetChild(1), Is.EqualTo(0x72828282));

            node = new CompactTreeNode(node.RawData);

            Assert.That(node.IsDataNode, Is.False);
            Assert.That(node.IsSplitNode, Is.True);
            Assert.That(node.GetChild(0), Is.EqualTo(0x71818181));
            Assert.That(node.GetChild(1), Is.EqualTo(0x72828282));

            node = node.SetChild(0, 0x7FFFFFFF);
            node = node.SetChild(1, 0x7FFFFFFF);

            Assert.That(node.IsDataNode, Is.False);
            Assert.That(node.IsSplitNode, Is.True);
            Assert.That(node.GetChild(0), Is.EqualTo(0x7FFFFFFF));
            Assert.That(node.GetChild(1), Is.EqualTo(0x7FFFFFFF));

            node = node.SetChild(0, 0);
            node = node.SetChild(1, 0);

            Assert.That(node.IsDataNode, Is.False);
            Assert.That(node.IsSplitNode, Is.True);
            Assert.That(node.GetChild(0), Is.EqualTo(0));
            Assert.That(node.GetChild(1), Is.EqualTo(0));
        }
    }
}