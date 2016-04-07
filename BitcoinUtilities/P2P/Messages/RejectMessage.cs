namespace BitcoinUtilities.P2P.Messages
{
    /// <summary>
    /// The reject message is sent when messages are rejected.
    /// </summary>
    public class RejectMessage : IBitcoinMessage
    {
        public const string Command = "reject";

        public enum RejectReason
        {
            Malformed = 0x01,
            Invalid = 0x10,
            Obsolete = 0x11,
            Duplicate = 0x12,
            Nonstandard = 0x40,
            Dust = 0x41,
            InsufficientFee = 0x42,
            Checkpoint = 0x43
        }

        private const int MaxTextLength = 16*1024;

        private readonly string rejectedCommand;
        private readonly RejectReason reason;
        private readonly string reasonText;
        private byte[] data;

        public RejectMessage(string rejectedCommand, RejectReason reason, string reasonText)
        {
            this.rejectedCommand = rejectedCommand;
            this.reason = reason;
            this.reasonText = reasonText;
        }

        /// <summary>
        /// Type of message rejected.
        /// </summary>
        public string RejectedCommand
        {
            get { return rejectedCommand; }
        }

        /// <summary>
        /// Code relating to rejected message.
        /// </summary>
        public RejectReason Reason
        {
            get { return reason; }
        }

        /// <summary>
        /// Text version of reason for rejection.
        /// </summary>
        public string ReasonText
        {
            get { return reasonText; }
        }

        /// <summary>
        /// Optional extra data provided by some errors.
        /// <para/>
        /// Currently, all errors which provide this field fill it with the TXID or block header hash of the object being rejected, so the field is 32 bytes.
        /// </summary>
        public byte[] Data
        {
            get { return data; }
            private set { data = value; }
        }

        string IBitcoinMessage.Command
        {
            get { return Command; }
        }

        public void Write(BitcoinStreamWriter writer)
        {
            writer.WriteText(rejectedCommand);
            writer.Write((byte) reason);
            writer.WriteText(reasonText);
            if (data != null)
            {
                writer.Write(data);
            }
        }

        public static RejectMessage Read(BitcoinStreamReader reader)
        {
            string rejectedCommand = reader.ReadText(MaxTextLength);
            byte reasonByte = reader.ReadByte();
            string reasonText = reader.ReadText(MaxTextLength);
            //todo: parse data? length should be provided as parameter? 

            RejectReason reason = (RejectReason) reasonByte;

            return new RejectMessage(rejectedCommand, reason, reasonText);
        }
    }
}