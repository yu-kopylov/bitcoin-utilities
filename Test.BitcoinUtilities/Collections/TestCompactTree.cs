using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.Collections;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Collections
{
    [TestFixture]
    public class TestCompactTree
    {
        [Test]
        public void TestCopy()
        {
            CompactTree originalTree = new CompactTree(CompactTreeNode.CreateSplitNode());
            int data1Index = originalTree.AddDataNode(0, 0, 0x5555555555555555);
            int splitIndex = originalTree.AddSplitNode(0, 1);
            int data2Index = originalTree.AddDataNode(splitIndex, 0, 0x2AAAAAAAAAAAAAAA);

            List<ulong> serializedTree = originalTree.Nodes.Select(n => n.RawData).ToList();
            CompactTree copiedTree = new CompactTree();
            copiedTree.Nodes.AddRange(serializedTree.Select(v => new CompactTreeNode(v)));

            CompactTreeNode rootNode = originalTree.Nodes[0];
            CompactTreeNode data1Node = originalTree.Nodes[data1Index];
            CompactTreeNode splitNode = originalTree.Nodes[splitIndex];
            CompactTreeNode data2Node = originalTree.Nodes[data2Index];

            Assert.That(rootNode.IsSplitNode);
            Assert.That(rootNode.GetChild(0), Is.EqualTo(data1Index));
            Assert.That(rootNode.GetChild(1), Is.EqualTo(splitIndex));

            Assert.That(data1Node.IsDataNode);
            Assert.That(data1Node.Value, Is.EqualTo(0x5555555555555555));

            Assert.That(splitNode.IsSplitNode);
            Assert.That(splitNode.GetChild(0), Is.EqualTo(data2Index));
            Assert.That(splitNode.GetChild(1), Is.EqualTo(0));

            Assert.That(data2Node.IsDataNode);
            Assert.That(data2Node.Value, Is.EqualTo(0x2AAAAAAAAAAAAAAA));
        }

        [Test]
        public void TestTwoBranches()
        {
            const int brachesLength = 1000;

            CompactTree tree = new CompactTree(CompactTreeNode.CreateSplitNode());

            int leftNodeIndex = 0;
            int rightNodeIndex = 0;

            for (int i = 1; i < brachesLength; i++)
            {
                leftNodeIndex = tree.AddSplitNode(leftNodeIndex, 0);
                rightNodeIndex = tree.AddSplitNode(rightNodeIndex, 1);
            }

            tree.AddDataNode(leftNodeIndex, 0, 0x1);
            tree.AddDataNode(rightNodeIndex, 1, 0x2);

            leftNodeIndex = 0;
            rightNodeIndex = 0;

            for (int i = 0; i < brachesLength; i++)
            {
                CompactTreeNode leftNode = tree.Nodes[leftNodeIndex];
                CompactTreeNode rightNode = tree.Nodes[rightNodeIndex];

                Assert.That(leftNode.IsSplitNode);
                Assert.That(rightNode.IsSplitNode);

                if (i > 0)
                {
                    Assert.That(leftNode.GetChild(1), Is.EqualTo(0));
                    Assert.That(rightNode.GetChild(0), Is.EqualTo(0));
                }

                leftNodeIndex = leftNode.GetChild(0);
                rightNodeIndex = rightNode.GetChild(1);
            }

            CompactTreeNode leftDataNode = tree.Nodes[leftNodeIndex];
            CompactTreeNode rightDataNode = tree.Nodes[rightNodeIndex];

            Assert.That(leftDataNode.IsDataNode);
            Assert.That(rightDataNode.IsDataNode);

            Assert.That(leftDataNode.Value, Is.EqualTo(1));
            Assert.That(rightDataNode.Value, Is.EqualTo(2));
        }

        [Test]
        public void TestBalancedTree()
        {
            const int depth = 12;

            const int expectedNodesCount = (1 << depth) - 1;
            const int valuesCount = 1 << (depth - 1);
            const int startingMask = 1 << (depth - 2);

            CompactTree tree = BuildBalancedTree(depth);
            Assert.That(tree.Nodes.Count, Is.EqualTo(expectedNodesCount));

            for (int i = 0; i < valuesCount; i++)
            {
                int nodeIndex = 0;

                int mask = startingMask;
                while (mask > 0)
                {
                    CompactTreeNode node = tree.Nodes[nodeIndex];
                    Assert.That(node.IsSplitNode);

                    //todo: use bool
                    bool left = (i & mask) == 0;
                    nodeIndex = node.GetChild(left ? 0 : 1);

                    mask >>= 1;
                }

                CompactTreeNode valueNode = tree.Nodes[nodeIndex];
                Assert.That(valueNode.IsDataNode);
                Assert.That(valueNode.Value, Is.EqualTo(i));
            }
        }

        private CompactTree BuildBalancedTree(int depth)
        {
            CompactTree tree = new CompactTree(CompactTreeNode.CreateSplitNode());

            List<int> path = new List<int>();

            path.Add(0);

            int nextNodeValue = 0;

            while (path.Count > 0)
            {
                int nodeIndex = path.Last();

                CompactTreeNode node = tree.Nodes[nodeIndex];

                if (path.Count + 1 >= depth)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        tree.AddDataNode(nodeIndex, i, nextNodeValue++);
                    }
                    path.RemoveAt(path.Count - 1);
                }
                else
                {
                    bool nodeAdded = false;
                    for (int i = 0; i < 2 && !nodeAdded; i++)
                    {
                        if (node.GetChild(i) == 0)
                        {
                            int childIndex = tree.AddSplitNode(nodeIndex, i);
                            path.Add(childIndex);
                            nodeAdded = true;
                        }
                    }
                    if (!nodeAdded)
                    {
                        path.RemoveAt(path.Count - 1);
                    }
                }
            }

            return tree;
        }
    }
}