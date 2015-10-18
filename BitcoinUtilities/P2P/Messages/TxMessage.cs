using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// <see cref="TxMessage"/> describes a bitcoin transaction, in reply to <see cref="GetDataMessage"/>.
    /// </summary>
    public class TxMessage : IBitcoinMessage
    {
        public const string Command = "block";

        private readonly Tx transaction;

        private TxMessage(Tx transaction)
        {
            this.transaction = transaction;
        }

        /// <summary>
        /// A transaction.
        /// </summary>
        public Tx Transaction
        {
            get { return transaction; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            transaction.Write(writer);
        }

        public static TxMessage Read(BitcoinStreamReader reader)
        {
            Tx transaction = Tx.Read(reader);
            return new TxMessage(transaction);
        }
    }
}