namespace BitcoinUtilities.Node.Services.Outputs
{
    public class UtxoOutput
    {
        public UtxoOutput(byte[] outputPoint, int height, ulong value, byte[] script, int spentHeight)
        {
            OutputPoint = outputPoint;
            Height = height;
            Value = value;
            Script = script;
            SpentHeight = spentHeight;
        }

        public byte[] OutputPoint { get; }
        public int Height { get; }
        public ulong Value { get; }
        public byte[] Script { get; }
        public int SpentHeight { get; }

        public UtxoOutput Spend(int spentHeight)
        {
            return new UtxoOutput(OutputPoint, Height, Value, Script, spentHeight);
        }
    }
}