﻿using System;
using System.Collections.Generic;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// Allows a node to advertise its knowledge of one or more objects. It can be received unsolicited, or in reply to <see cref="GetBlocksMessage"/>.
    /// </summary>
    public class InvMessage : IBitcoinMessage
    {
        public const string Command = "inv";

        private readonly List<InventoryVector> inventory;

        public InvMessage(InventoryVector[] inventory)
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

        public static InvMessage Read(BitcoinStreamReader reader)
        {
            ulong count = reader.ReadUInt64Compact();

            if (count > 10000)
            {
                //todo: handle correctly
                throw new Exception("Too many inventory vectors.");
            }

            InventoryVector[] values = new InventoryVector[count];
            for (ulong i = 0; i < count; i++)
            {
                values[i] = InventoryVector.Read(reader);
            }
            return new InvMessage(values);
        }
    }
}