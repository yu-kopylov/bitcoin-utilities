using System.Collections.Generic;

namespace BitcoinUtilities.Scripts
{
    /// <summary>
    /// A parser for Bitcoin script.
    /// </summary>
    public class ScriptParser
    {
        /// <summary>
        /// Parses a given array of bytes.
        /// </summary>
        /// <param name="script">The array of bytes with a script.</param>
        /// <param name="commands">A list of parsed commands; or null if parsing failed.</param>
        /// <returns>true if script was parsed successfully; otherwise, false.</returns>
        public bool TryParse(byte[] script, out List<ScriptCommand> commands)
        {
            int offset = 0;

            commands = new List<ScriptCommand>();

            while (offset < script.Length)
            {
                ScriptCommand command;
                if (!TryReadCommand(script, offset, out command))
                {
                    commands = null;
                    return false;
                }
                commands.Add(command);
                offset += command.Length;
            }

            return true;
        }

        private bool TryReadCommand(byte[] script, int offset, out ScriptCommand command)
        {
            byte code = script[offset];
            int length = 1;
            if (code >= BitcoinScript.OP_PUSHDATA_LEN_1 && code <= BitcoinScript.OP_PUSHDATA_LEN_75)
            {
                length = 1 + code;
            }
            else if (code == BitcoinScript.OP_PUSHDATA1)
            {
                if (offset + 1 >= script.Length)
                {
                    command = default(ScriptCommand);
                    return false;
                }
                int dataLength = script[offset + 1];
                length = 2 + dataLength;
            }
            else if (code == BitcoinScript.OP_PUSHDATA2)
            {
                if (offset + 2 >= script.Length)
                {
                    command = default(ScriptCommand);
                    return false;
                }

                int dataLength = script[offset + 2];
                dataLength = dataLength*256 + script[offset + 1];

                length = 3 + dataLength;
            }
            else if (code == BitcoinScript.OP_PUSHDATA4)
            {
                if (offset + 4 >= script.Length)
                {
                    command = default(ScriptCommand);
                    return false;
                }

                int dataLength = script[offset + 4];
                dataLength = dataLength*256 + script[offset + 3];
                dataLength = dataLength*256 + script[offset + 2];
                dataLength = dataLength*256 + script[offset + 1];

                if (dataLength < 0)
                {
                    command = default(ScriptCommand);
                    return false;
                }

                length = 5 + dataLength;
            }

            if (offset + length > script.Length)
            {
                command = default(ScriptCommand);
                return false;
            }

            command = new ScriptCommand(code, offset, length);
            return true;
        }
    }
}