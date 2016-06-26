using System;

namespace BitcoinUtilities.Scripts
{
    public static class BitcoinScript
    {
        // ---------------------------------------------------------------------------------------
        // ---- Constants ------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// An empty array of bytes is pushed onto the stack.
        /// </summary>
        public const byte OP_FALSE = 0x00;

        /// <summary>
        /// The next opcode bytes is data to be pushed onto the stack.
        /// </summary>
        public const byte OP_PUSHDATA_LEN_1 = 0x01;

        /// <summary>
        /// The next opcode bytes is data to be pushed onto the stack.
        /// </summary>
        public const byte OP_PUSHDATA_LEN_75 = 0x4b;

        /// <summary>
        /// The next byte contains the number of bytes to be pushed onto the stack.
        /// </summary>
        public const byte OP_PUSHDATA1 = 0x4c;

        /// <summary>
        /// The next two bytes contain the number of bytes to be pushed onto the stack.
        /// </summary>
        public const byte OP_PUSHDATA2 = 0x4d;

        /// <summary>
        /// The next four bytes contain the number of bytes to be pushed onto the stack.
        /// </summary>
        public const byte OP_PUSHDATA4 = 0x4e;

        /// <summary>
        /// The number -1 is pushed onto the stack.
        /// </summary>
        public const byte OP_1NEGATE = 0x4f;

        /// <summary>
        /// The number 1 is pushed onto the stack.
        /// </summary>
        public const byte OP_TRUE = 0x51;

        /// <summary>
        /// The number 2 is pushed onto the stack.
        /// </summary>
        public const byte OP_2 = 0x52;

        /// <summary>
        /// The number 3 is pushed onto the stack.
        /// </summary>
        public const byte OP_3 = 0x53;

        /// <summary>
        /// The number 4 is pushed onto the stack.
        /// </summary>
        public const byte OP_4 = 0x54;

        /// <summary>
        /// The number 5 is pushed onto the stack.
        /// </summary>
        public const byte OP_5 = 0x55;

        /// <summary>
        /// The number 16 is pushed onto the stack.
        /// </summary>
        public const byte OP_16 = 0x60;

        // ---------------------------------------------------------------------------------------
        // ---- Flow control ---------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Does nothing.
        /// </summary>
        public const byte OP_NOP = 0x61;

        /// <summary>
        /// If the top stack value is not 0, the statements are executed. The top stack value is removed.
        /// </summary>
        public const byte OP_IF = 0x63;

        /// <summary>
        /// If the top stack value is 0, the statements are executed. The top stack value is removed.
        /// </summary>
        public const byte OP_NOTIF = 0x64;

        /// <summary>
        /// If the preceding OP_IF or OP_NOTIF or OP_ELSE was not executed then these statements are
        /// and if the preceding OP_IF or OP_NOTIF or OP_ELSE was executed then these statements are not.
        /// </summary>
        public const byte OP_ELSE = 0x67;

        /// <summary>
        /// Ends an if/else block. All blocks must end, or the transaction is invalid. An OP_ENDIF without OP_IF earlier is also invalid.
        /// </summary>
        public const byte OP_ENDIF = 0x68;

        /// <summary>
        /// Marks transaction as invalid if top stack value is not true.
        /// </summary>
        public const byte OP_VERIFY = 0x69;

        /// <summary>
        /// Marks transaction as invalid.
        /// A standard way of attaching extra data to transactions is to add a zero-value output with a scriptPubKey
        /// consisting of OP_RETURN followed by exactly one pushdata op.
        /// Such outputs are provably unspendable, reducing their cost to the network.
        /// Currently it is usually considered non-standard (though valid) for a transaction to have more than one OP_RETURN output
        /// or an OP_RETURN output with more than one pushdata op.
        /// </summary>
        public const byte OP_RETURN = 0x6a;

        // ---------------------------------------------------------------------------------------
        // ---- Stack ----------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Duplicates the top stack item.
        /// </summary>
        public const byte OP_DUP = 0x76;

        // ---------------------------------------------------------------------------------------
        // ---- Bitwise logic --------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Returns 1 if the inputs are exactly equal, 0 otherwise.
        /// </summary>
        public const byte OP_EQUAL = 0x87;

        /// <summary>
        /// Same as OP_EQUAL, but runs OP_VERIFY afterward.
        /// </summary>
        public const byte OP_EQUALVERIFY = 0x88;

        // ---------------------------------------------------------------------------------------
        // ---- Crypto ---------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The input is hashed twice: first with SHA-256 and then with RIPEMD-160.
        /// </summary>
        public const byte OP_HASH160 = 0xA9;

        /// <summary>
        /// The entire transaction's outputs, inputs, and script (from the most recently-executed OP_CODESEPARATOR to the end) are hashed.
        /// The signature used by OP_CHECKSIG must be a valid signature for this hash and public key. If it is, 1 is returned, 0 otherwise.
        /// </summary>
        public const byte OP_CHECKSIG = 0xAC;

        /// <summary>
        /// Extracts an address from the given pubkey script if it has a known format.
        /// </summary>
        /// <param name="pubkeyScript">The array of bytes with a pubkey script.</param>
        /// <returns>An address in the Base58Check encoding if it was extracted successfully; otherwise, null.</returns>
        public static string GetAddressFromPubkeyScript(byte[] pubkeyScript)
        {
            if (pubkeyScript == null)
            {
                return null;
            }

            if (IsPayToPubkeyHash(pubkeyScript))
            {
                //todo: set first byte according to the Network type (0x00 for Main Network)
                byte[] address = new byte[21];
                Array.Copy(pubkeyScript, 3, address, 1, 20);
                return Base58Check.Encode(address);
            }

            return null;
        }

        /// <summary>
        /// Tests if the given input is a pay-to-pubkey-hash script.
        /// </summary>
        /// <param name="pubkeyScript">The array of bytes to test.</param>
        /// <returns>true if the given input is a pay-to-pubkey-hash script; otherwise false.</returns>
        public static bool IsPayToPubkeyHash(byte[] pubkeyScript)
        {
            if (pubkeyScript == null || pubkeyScript.Length != 25)
            {
                return false;
            }

            return
                pubkeyScript[0] == OP_DUP &&
                pubkeyScript[1] == OP_HASH160 &&
                pubkeyScript[2] == 20 &&
                pubkeyScript[23] == OP_EQUALVERIFY &&
                pubkeyScript[24] == OP_CHECKSIG;
        }
    }
}