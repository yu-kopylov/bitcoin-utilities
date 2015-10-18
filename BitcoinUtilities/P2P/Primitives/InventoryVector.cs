using System.IO;

namespace BitcoinUtilities.P2P.Primitives
{
    /// <summary>
    /// Inventory vectors are used for notifying other nodes about objects they have or data which is being requested.
    /// </summary>
    public struct InventoryVector
    {
        private readonly InventoryVectorType type;
        private readonly byte[] hash;

        public InventoryVector(InventoryVectorType type, byte[] hash)
        {
            this.type = type;
            this.hash = hash;
        }

        /// <summary>
        /// Identifies the object type linked to this inventory
        /// </summary>
        public InventoryVectorType Type
        {
            get { return type; }
        }

        /// <summary>
        /// Hash of the object
        /// </summary>
        public byte[] Hash
        {
            get { return hash; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.Write((int) type);
            writer.Write(hash);
        }

        public static InventoryVector Read(BitcoinStreamReader reader)
        {
            InventoryVectorType type = (InventoryVectorType) reader.ReadInt32();
            byte[] hash = reader.ReadBytes(32);
            return new InventoryVector(type, hash);
        }
    }
}