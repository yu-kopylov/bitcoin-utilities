using System;
using System.Collections.Generic;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// <see cref="GetDataMessage"/> is used in response to <see cref="InvMessage"/>, to retrieve the content of a specific object, and is usually sent after receiving an <see cref="InvMessage"/>, after filtering known elements.
    /// It can be used to retrieve transactions, but only if they are in the memory pool or relay set
    /// - arbitrary access to transactions in the chain is not allowed to avoid having clients start to depend on nodes having full transaction indexes (which modern nodes do not).
    /// <para/>
    /// Payload (maximum 50,000 entries, which is just over 1.8 megabytes):
    /// </summary>
    public class GetDataMessage : IBitcoinMessage
    {
        public const string Command = "getdata";

        private readonly List<InventoryVector> inventory;

        public GetDataMessage(InventoryVector[] inventory)
        {
            this.inventory = new List<InventoryVector>(inventory);
        }

        /// <summary>
        /// Inventory vectors
        /// </summary>
        public List<InventoryVector> Inventory
        {
            get { return inventory; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.WriteCompact((ulong) inventory.Count);
            foreach (InventoryVector vector in inventory)
            {
                vector.Write(writer);
            }
        }

        public static GetDataMessage Read(BitcoinStreamReader reader)
        {
            ulong count = reader.ReadUInt64Compact();
            if (count > 50000)
            {
                //todo: handle correctly
                throw new Exception("Too many inventory vectors.");
            }
            
            InventoryVector[] inventory = new InventoryVector[count];
            for (ulong i = 0; i < count; i++)
            {
                inventory[i] = InventoryVector.Read(reader);
            }

            return new GetDataMessage(inventory);
        }
    }
}