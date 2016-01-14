using System;

namespace BitcoinUtilities.Scripts
{
    public static class BitcoinScript
    {
        // ---------------------------------------------------------------------------------------
        // ---- Constants ------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The next opcode bytes is data to be pushed onto the stack.
        /// </summary>
        public const byte OP_PUSHBYTES_MIN = 0x01;

        /// <summary>
        /// The next opcode bytes is data to be pushed onto the stack.
        /// </summary>
        public const byte OP_PUSHBYTES_MAX = 0x4b;

        /// <summary>
        /// Marks transaction as invalid if top stack value is not true.
        /// </summary>
        public const byte OP_TRUE = 0x51;

        // ---------------------------------------------------------------------------------------
        // ---- Flow control ---------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Marks transaction as invalid if top stack value is not true.
        /// </summary>
        public const byte OP_VERIFY = 0x69;

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