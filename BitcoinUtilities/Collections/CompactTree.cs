using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace BitcoinUtilities.Collections
{
    //todo: add XMLDOC
    public class CompactTree
    {
        private readonly List<CompactTreeNode> nodes = new List<CompactTreeNode>();

        public CompactTree()
        {
        }

        public CompactTree(CompactTreeNode rootNode)
        {
            Add(rootNode);
        }

        /// <summary>
        /// A list of all nodes in the tree.
        /// <para/>
        /// Can be used for fast serialization and deserialization.
        /// </summary>
        public List<CompactTreeNode> Nodes
        {
            get { return nodes; }
        }

        public int AddSplitNode(int parentIndex, int childNum)
        {
            CompactTreeNode parentNode = nodes[parentIndex];
            //todo: contract or specific exception?
            Contract.Assert(parentNode.IsSplitNode);

            CompactTreeNode childNode = CompactTreeNode.CreateSplitNode();
            int childIndex = Add(childNode);

            parentNode = parentNode.SetChild(childNum, childIndex);
            nodes[parentIndex] = parentNode;

            return childIndex;
        }

        public int AddDataNode(int parentIndex, int childNum, long value)
        {
            CompactTreeNode parentNode = nodes[parentIndex];
            Contract.Assert(parentNode.IsSplitNode);

            CompactTreeNode childNode = CompactTreeNode.CreateDataNode(value);
            int childIndex = Add(childNode);

            parentNode = parentNode.SetChild(childNum, childIndex);
            nodes[parentIndex] = parentNode;

            return childIndex;
        }

        private int Add(CompactTreeNode node)
        {
            int idx = nodes.Count;
            //todo: add to XMLDOC ?
            //Contract.Assert(idx < 0x8000000, "Arrays longer than 1GB are not suppported by default by the .NET Framework.");
            nodes.Add(node);
            return idx;
        }
    }
}