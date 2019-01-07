namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoOutput
    {
        public UtxoOutput(byte[] outputPoint, int height, ulong value, byte[] script)
        {
            OutputPoint = outputPoint;
            Height = height;
            Value = value;
            Script = script;
        }

        public byte[] OutputPoint { get; }
        public int Height { get; }
        public ulong Value { get; }
        public byte[] Script { get; }
    }
}