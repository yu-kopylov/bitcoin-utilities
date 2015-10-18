using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The block message is sent in response to a <see cref="GetDataMessage"/> message which requests transaction information from a block hash.
    /// <para/>
    /// The block message transmits a single serialized block in the format described in the serialized blocks section.
    /// </summary>
    public class BlockMessage : IBitcoinMessage
    {
        public const string Command = "block";

        private readonly BlockHeader blockHeader;
        //todo: decide on list vs array
        private readonly Tx[] transactions;

        private BlockMessage(BlockHeader blockHeader, Tx[] transactions)
        {
            this.blockHeader = blockHeader;
            this.transactions = transactions;
        }

        /// <summary>
        /// The block header.
        /// </summary>
        public BlockHeader BlockHeader
        {
            get { return blockHeader; }
        }

        /// <summary>
        /// Block transactions, in format of "tx" command.
        /// </summary>
        public Tx[] Transactions
        {
            get { return transactions; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            blockHeader.Write(writer);
            writer.WriteArray(transactions, (w, t) => t.Write(w));
        }

        public static BlockMessage Read(BitcoinStreamReader reader)
        {
            BlockHeader blockHeader = BlockHeader.Read(reader);

            //todo: see if there is actual limitation for this field 
            Tx[] transactions = reader.ReadArray(1024 * 1024, Tx.Read);

            return new BlockMessage(blockHeader, transactions);
        }
    }
}