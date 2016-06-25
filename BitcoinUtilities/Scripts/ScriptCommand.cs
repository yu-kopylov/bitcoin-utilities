namespace BitcoinUtilities.Scripts
{
    public struct ScriptCommand
    {
        public ScriptCommand(byte code, int offset, int length)
        {
            Code = code;
            Offset = offset;
            Length = length;
        }

        public byte Code { get; }
        public int Offset { get; }
        public int Length { get; }
    }
}