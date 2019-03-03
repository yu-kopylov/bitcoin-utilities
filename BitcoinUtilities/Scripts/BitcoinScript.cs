using System;
using System.Diagnostics.CodeAnalysis;

namespace BitcoinUtilities.Scripts
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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
        public const byte OP_PUSHDATA_LEN_75 = 0x4B;

        /// <summary>
        /// The next byte contains the number of bytes to be pushed onto the stack.
        /// </summary>
        public const byte OP_PUSHDATA1 = 0x4C;

        /// <summary>
        /// The next two bytes contain the number of bytes to be pushed onto the stack in little-endian order.
        /// </summary>
        public const byte OP_PUSHDATA2 = 0x4D;

        /// <summary>
        /// The next four bytes contain the number of bytes to be pushed onto the stack in little-endian order.
        /// </summary>
        public const byte OP_PUSHDATA4 = 0x4E;

        /// <summary>
        /// The number -1 is pushed onto the stack.
        /// </summary>
        public const byte OP_1NEGATE = 0x4F;

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
        public const byte OP_RETURN = 0x6A;

        // ---------------------------------------------------------------------------------------
        // ---- Stack ----------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Puts the input onto the top of the alt stack.Removes it from the main stack.
        /// </summary>
        public const byte OP_TOALTSTACK = 0x6B;

        /// <summary>
        /// Puts the input onto the top of the main stack.Removes it from the alt stack.
        /// </summary>
        public const byte OP_FROMALTSTACK = 0x6C;

        /// <summary>
        /// Removes the top two stack items.
        /// </summary>
        public const byte OP_2DROP = 0x6D;

        /// <summary>
        /// Duplicates the top two stack items.
        /// </summary>
        public const byte OP_2DUP = 0x6E;

        /// <summary>
        /// Duplicates the top three stack items.
        /// </summary>
        public const byte OP_3DUP = 0x6F;

        /// <summary>
        /// Copies the pair of items two spaces back in the stack to the front.
        /// </summary>
        public const byte OP_2OVER = 0x70;

        /// <summary>
        /// The fifth and sixth items back are moved to the top of the stack.
        /// </summary>
        public const byte OP_2ROT = 0x71;

        /// <summary>
        /// Swaps the top two pairs of items.
        /// </summary>
        public const byte OP_2SWAP = 0x72;

        /// <summary>
        /// If the top stack value is not 0, duplicate it.
        /// </summary>
        public const byte OP_IFDUP = 0x73;

        /// <summary>
        /// Puts the number of stack items onto the stack.
        /// </summary>
        public const byte OP_DEPTH = 0x74;

        /// <summary>
        /// Removes the top stack item.
        /// </summary>
        public const byte OP_DROP = 0x75;

        /// <summary>
        /// Duplicates the top stack item.
        /// </summary>
        public const byte OP_DUP = 0x76;

        /// <summary>
        /// Removes the second-to-top stack item.
        /// </summary>
        public const byte OP_NIP = 0x77;

        /// <summary>
        /// Copies the second-to-top stack item to the top.
        /// </summary>
        public const byte OP_OVER = 0x78;

        /// <summary>
        /// The item n back in the stack is copied to the top.
        /// </summary>
        public const byte OP_PICK = 0x79;

        /// <summary>
        /// The item n back in the stack is moved to the top.
        /// </summary>
        public const byte OP_ROLL = 0x7A;

        /// <summary>
        /// The top three items on the stack are rotated to the left.
        /// </summary>
        public const byte OP_ROT = 0x7B;

        /// <summary>
        /// The top two items on the stack are swapped.
        /// </summary>
        public const byte OP_SWAP = 0x7C;

        /// <summary>
        /// The item at the top of the stack is copied and inserted before the second-to-top item.
        /// </summary>
        public const byte OP_TUCK = 0x7D;

        // ---------------------------------------------------------------------------------------
        // ---- Splice ---------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Concatenates two strings.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_CAT = 0x7E;

        /// <summary>
        /// Returns a section of a string.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_SUBSTR = 0x7F;

        /// <summary>
        /// Keeps only characters left of the specified point in a string.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_LEFT = 0x80;

        /// <summary>
        /// Keeps only characters right of the specified point in a string.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_RIGHT = 0x81;

        /// <summary>
        /// Pushes the string length of the top element of the stack (without popping it).
        /// </summary>
        public const byte OP_SIZE = 0x82;

        // ---------------------------------------------------------------------------------------
        // ---- Bitwise Logic --------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Flips all of the bits in the input.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_INVERT = 0x83;

        /// <summary>
        /// Boolean and between each bit in the inputs.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_AND = 0x84;

        /// <summary>
        /// Boolean or between each bit in the inputs.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_OR = 0x85;

        /// <summary>
        /// Boolean exclusive or between each bit in the inputs.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_XOR = 0x86;

        /// <summary>
        /// Returns 1 if the inputs are exactly equal, 0 otherwise.
        /// </summary>
        public const byte OP_EQUAL = 0x87;

        /// <summary>
        /// Same as <see cref="OP_EQUAL"/>, but runs <see cref="OP_VERIFY"/> afterwards.
        /// </summary>
        public const byte OP_EQUALVERIFY = 0x88;

        // ---------------------------------------------------------------------------------------
        // ---- Arithmetic -----------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------
        // Note: Arithmetic inputs are limited to signed 32-bit integers, but may overflow their output.
        // If any input value for any of these commands is longer than 4 bytes, the script must abort and fail.
        // If any opcode marked as disabled is present in a script - it must also abort and fail.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// 1 is added to the input.
        /// </summary>
        public const byte OP_1ADD = 0x8B;

        /// <summary>
        /// 1 is subtracted from the input.
        /// </summary>
        public const byte OP_1SUB = 0x8C;

        /// <summary>
        /// The input is multiplied by 2.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_2MUL = 0x8D;

        /// <summary>
        /// The input is divided by 2.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_2DIV = 0x8E;

        /// <summary>
        /// The sign of the input is flipped.
        /// </summary>
        public const byte OP_NEGATE = 0x8F;

        /// <summary>
        /// The input is made positive.
        /// </summary>
        public const byte OP_ABS = 0x90;

        /// <summary>
        /// If the input is 0 or 1, it is flipped. Otherwise the output will be 0.
        /// </summary>
        public const byte OP_NOT = 0x91;

        /// <summary>
        /// Returns 0 if the input is 0. 1 otherwise.
        /// </summary>
        public const byte OP_0NOTEQUAL = 0x92;

        /// <summary>
        /// a is added to b.
        /// </summary>
        public const byte OP_ADD = 0x93;

        /// <summary>
        /// b is subtracted from a.
        /// </summary>
        public const byte OP_SUB = 0x94;

        /// <summary>
        /// a is multiplied by b.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_MUL = 0x95;

        /// <summary>
        /// a is divided by b.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_DIV = 0x96;

        /// <summary>
        /// Returns the remainder after dividing a by b.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_MOD = 0x97;

        /// <summary>
        /// Shifts a left b bits, preserving sign.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_LSHIFT = 0x98;

        /// <summary>
        /// Shifts a right b bits, preserving sign.
        /// </summary>
        /// <remarks>
        /// This command is disabled.
        /// </remarks>
        public const byte OP_RSHIFT = 0x99;

        /// <summary>
        /// If both a and b are not "" (null string), the output is 1. Otherwise 0.
        /// </summary>
        public const byte OP_BOOLAND = 0x9A;

        /// <summary>
        /// If a or b is not "" (null string), the output is 1. Otherwise 0.
        /// </summary>
        public const byte OP_BOOLOR = 0x9B;

        /// <summary>
        /// Returns 1 if the numbers are equal, 0 otherwise.
        /// </summary>
        public const byte OP_NUMEQUAL = 0x9C;

        /// <summary>
        /// Same as OP_NUMEQUAL, but runs OP_VERIFY afterward.
        /// </summary>
        public const byte OP_NUMEQUALVERIFY = 0x9D;

        /// <summary>
        /// Returns 1 if the numbers are not equal, 0 otherwise.
        /// </summary>
        public const byte OP_NUMNOTEQUAL = 0x9E;

        /// <summary>
        /// Returns 1 if a is less than b, 0 otherwise.
        /// </summary>
        public const byte OP_LESSTHAN = 0x9F;

        /// <summary>
        /// Returns 1 if a is greater than b, 0 otherwise.
        /// </summary>
        public const byte OP_GREATERTHAN = 0xA0;

        /// <summary>
        /// Returns 1 if a is less than or equal to b, 0 otherwise.
        /// </summary>
        public const byte OP_LESSTHANOREQUAL = 0xA1;

        /// <summary>
        /// Returns 1 if a is greater than or equal to b, 0 otherwise.
        /// </summary>
        public const byte OP_GREATERTHANOREQUAL = 0xA2;

        /// <summary>
        /// Returns the smaller of a and b.
        /// </summary>
        public const byte OP_MIN = 0xA3;

        /// <summary>
        /// Returns the larger of a and b.
        /// </summary>
        public const byte OP_MAX = 0xA4;

        /// <summary>
        /// Returns 1 if x is within the specified range (left-inclusive), 0 otherwise.
        /// </summary>
        public const byte OP_WITHIN = 0xA5;

        // ---------------------------------------------------------------------------------------
        // ---- Crypto ---------------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The input is hashed using RIPEMD-160.
        /// </summary>
        public const byte OP_RIPEMD160 = 0xA6;

        /// <summary>
        /// The input is hashed using SHA-1.
        /// </summary>
        public const byte OP_SHA1 = 0xA7;

        /// <summary>
        /// The input is hashed using SHA-256.
        /// </summary>
        public const byte OP_SHA256 = 0xA8;

        /// <summary>
        /// The input is hashed twice: first with SHA-256 and then with RIPEMD-160.
        /// </summary>
        public const byte OP_HASH160 = 0xA9;

        /// <summary>
        /// The input is hashed two times with SHA-256.
        /// </summary>
        public const byte OP_HASH256 = 0xAA;

        /// <summary>
        /// All of the signature checking words will only match signatures to the data after the most recently-executed OP_CODESEPARATOR.
        /// </summary>
        public const byte OP_CODESEPARATOR = 0xAB;

        /// <summary>
        /// The entire transaction's outputs, inputs, and script (from the most recently-executed <see cref="OP_CODESEPARATOR"/> to the end) are hashed.
        /// The signature used by OP_CHECKSIG must be a valid signature for this hash and public key. If it is, 1 is returned, 0 otherwise.
        /// </summary>
        public const byte OP_CHECKSIG = 0xAC;

        /// <summary>
        /// Same as <see cref="OP_CHECKSIG"/>, but <see cref="OP_VERIFY"/> is executed afterwards.
        /// </summary>
        public const byte OP_CHECKSIGVERIFY = 0xAD;

        /// <summary>
        /// Compares the first signature against each public key until it finds an ECDSA match.
        /// Starting with the subsequent public key, it compares the second signature against each remaining public key until it finds an ECDSA match.
        /// The process is repeated until all signatures have been checked or not enough public keys remain to produce a successful result.
        /// All signatures need to match a public key.
        /// <para/>
        /// Because public keys are not checked again if they fail any signature comparison,
        /// signatures must be placed in the scriptSig using the same order as their corresponding public keys were placed in the scriptPubKey or redeemScript.
        /// <para/>
        /// If all signatures are valid, 1 is returned, 0 otherwise. Due to a bug, one extra unused value is removed from the stack.
        /// </summary>
        public const byte OP_CHECKMULTISIG = 0xAE;

        /// <summary>
        /// Same as OP_CHECKMULTISIG, but OP_VERIFY is executed afterward.
        /// </summary>
        public const byte OP_CHECKMULTISIGVERIFY = 0xAF;

        // ---------------------------------------------------------------------------------------
        // ---- Reserved words -------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Transaction is invalid unless occuring in an unexecuted OP_IF branch.
        /// </summary>
        public const byte OP_RESERVED = 0x50;

        /// <summary>
        /// Transaction is invalid unless occuring in an unexecuted OP_IF branch.
        /// </summary>
        public const byte OP_VER = 0x62;

        /// <summary>
        /// Transaction is invalid even when occuring in an unexecuted OP_IF branch.
        /// </summary>
        public const byte OP_VERIF = 0x65;

        /// <summary>
        /// Transaction is invalid even when occuring in an unexecuted OP_IF branch.
        /// </summary>
        public const byte OP_VERNOTIF = 0x66;

        /// <summary>
        /// Transaction is invalid unless occuring in an unexecuted OP_IF branch.
        /// </summary>
        public const byte OP_RESERVED1 = 0x89;

        /// <summary>
        /// Transaction is invalid unless occuring in an unexecuted OP_IF branch.
        /// </summary>
        public const byte OP_RESERVED2 = 0x8A;

        /// <summary>
        /// The word is ignored. Does not mark transaction as invalid.
        /// </summary>
        public const byte OP_NOP1 = 0xB0;

        /// <summary>
        /// The word is ignored. Does not mark transaction as invalid.
        /// </summary>
        public const byte OP_NOP4 = 0xB3;

        /// <summary>
        /// The word is ignored. Does not mark transaction as invalid.
        /// </summary>
        public const byte OP_NOP5 = 0xB4;

        /// <summary>
        /// The word is ignored. Does not mark transaction as invalid.
        /// </summary>
        public const byte OP_NOP6 = 0xB5;

        /// <summary>
        /// The word is ignored. Does not mark transaction as invalid.
        /// </summary>
        public const byte OP_NOP7 = 0xB6;

        /// <summary>
        /// The word is ignored. Does not mark transaction as invalid.
        /// </summary>
        public const byte OP_NOP8 = 0xB7;

        /// <summary>
        /// The word is ignored. Does not mark transaction as invalid.
        /// </summary>
        public const byte OP_NOP9 = 0xB8;

        /// <summary>
        /// The word is ignored. Does not mark transaction as invalid.
        /// </summary>
        public const byte OP_NOP10 = 0xB9;

        // ---------------------------------------------------------------------------------------
        // ---- Pseudo-words ---------------------------------------------------------------------
        // ---------------------------------------------------------------------------------------
        // These words are used internally for assisting with transaction matching.
        // They are invalid if used in actual scripts.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Represents a public key hashed with OP_HASH160.
        /// </summary>
        public const byte OP_PUBKEYHASH = 0xFD;

        /// <summary>
        /// Represents a public key compatible with OP_CHECKSIG.
        /// </summary>
        public const byte OP_PUBKEY = 0xFE;

        /// <summary>
        /// Matches any opcode that is not yet assigned.
        /// </summary>
        public const byte OP_INVALIDOPCODE = 0xFF;

        /// <summary>
        /// Extracts an address from the given pubkey script if it has a known format.
        /// </summary>
        /// <param name="networkKind">The kind of a network the extracted address will be used for.</param>
        /// <param name="pubkeyScript">The array of bytes with a pubkey script.</param>
        /// <returns>An address in the Base58Check encoding if it was extracted successfully; otherwise, null.</returns>
        public static string GetAddressFromPubkeyScript(BitcoinNetworkKind networkKind, byte[] pubkeyScript)
        {
            // todo: remove this method
            if (pubkeyScript == null)
            {
                return null;
            }

            if (IsPayToPubkeyHash(pubkeyScript))
            {
                byte[] address = new byte[21];
                // todo: use code from network parameters
                address[0] = (byte) (networkKind == BitcoinNetworkKind.Main ? 0x00 : 0x6F);
                Array.Copy(pubkeyScript, 3, address, 1, 20);
                return Base58Check.Encode(address);
            }

            if (TryParsePayToPubkey(pubkeyScript, out byte[] publicKey))
            {
                // todo: use network parameters
                return BitcoinAddress.FromPublicKey(networkKind, publicKey);
            }

            return null;
        }

        /// <summary>
        /// Extracts a public key hash from the given pubkey script if it has a known format.
        /// </summary>
        /// <param name="pubkeyScript">The array of bytes with a pubkey script.</param>
        /// <returns>A public key hash if it was extracted successfully; otherwise, null.</returns>
        public static byte[] GetPublicKeyHashFromPubkeyScript(byte[] pubkeyScript)
        {
            //todo: add tests
            if (pubkeyScript == null)
            {
                return null;
            }

            //todo: add and use TryParsePayToPubkeyHash
            if (IsPayToPubkeyHash(pubkeyScript))
            {
                byte[] hash = new byte[20];
                Array.Copy(pubkeyScript, 3, hash, 0, 20);
                return hash;
            }

            if (TryParsePayToPubkey(pubkeyScript, out byte[] publicKey))
            {
                // todo: move CryptoUtils.RipeMd160(CryptoUtils.Sha256(...)) to utilities
                return CryptoUtils.RipeMd160(CryptoUtils.Sha256(publicKey));
            }

            return null;
        }

        /// <summary>
        /// Checks if the given pubkey script is a pay-to-pubkey-hash script.
        /// </summary>
        /// <param name="pubkeyScript">The pubkey script.</param>
        /// <returns>true if the given input is a pay-to-pubkey-hash script; otherwise, false.</returns>
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

        /// <summary>
        /// Checks if the given pubkey script is a pay-to-pubkey script.
        /// </summary>
        /// <remarks>
        /// This method does not check if public key has correct encoding.
        /// </remarks>
        /// <param name="pubkeyScript">The pubkey script.</param>
        /// <param name="publicKey">If script is a pay-to-pubkey script, contains the public key from it; otherwise, contains null.</param>
        /// <returns>true if the given input is a pay-to-pubkey script; otherwise, false.</returns>
        public static bool TryParsePayToPubkey(byte[] pubkeyScript, out byte[] publicKey)
        {
            publicKey = null;

            // Constants below are used to check both opcode and public key length at the same time.
            const int minPubkeyLength = 33;
            const int maxPubkeyLength = 65;

            if (pubkeyScript == null || pubkeyScript.Length == 0)
            {
                return false;
            }

            byte op1 = pubkeyScript[0];
            byte op2 = pubkeyScript[pubkeyScript.Length - 1];

            if (op2 != OP_CHECKSIG || op1 + 2 != pubkeyScript.Length || op1 < minPubkeyLength || op1 > maxPubkeyLength)
            {
                return false;
            }

            publicKey = new byte[op1];
            Array.Copy(pubkeyScript, 1, publicKey, 0, op1);
            return true;
        }

        // todo: add tests and xml-doc
        public static byte[] CreatePayToPubkeyHash(NetworkParameters networkParameters, string address)
        {
            if (!networkParameters.AddressConverter.TryGetPublicKeyHash(address, out var publicKeyHash))
            {
                throw new ArgumentException("Invalid address format.", nameof(address));
            }

            byte[] pubkeyScript = new byte[publicKeyHash.Length + 5];

            pubkeyScript[0] = OP_DUP;
            pubkeyScript[1] = OP_HASH160;
            pubkeyScript[2] = (byte) publicKeyHash.Length;
            Array.Copy(publicKeyHash, 0, pubkeyScript, 3, publicKeyHash.Length);
            pubkeyScript[publicKeyHash.Length + 3] = OP_EQUALVERIFY;
            pubkeyScript[publicKeyHash.Length + 4] = OP_CHECKSIG;

            return pubkeyScript;
        }

        // todo: add tests and xml-doc, validate parameters (including length up to OP_PUSHDATA_LEN_75)
        public static byte[] CreatePayToPubkeyHashSignature(SigHashType hashType, byte[] publicKey, byte[] signature)
        {
            byte[] script = new byte[3 + signature.Length + publicKey.Length];
            script[0] = (byte) (signature.Length + 1);
            Array.Copy(signature, 0, script, 1, signature.Length);
            script[1 + signature.Length] = (byte) hashType;
            script[2 + signature.Length] = (byte) publicKey.Length;
            Array.Copy(publicKey, 0, script, 3 + signature.Length, publicKey.Length);
            return script;
        }

        // todo: add tests and xml-doc, validate parameters (including length up to OP_PUSHDATA_LEN_75)
        public static byte[] CreatePayToPubkeySignature(SigHashType hashType, byte[] signature)
        {
            byte[] script = new byte[2 + signature.Length];
            script[0] = (byte) (signature.Length + 1);
            Array.Copy(signature, 0, script, 1, signature.Length);
            script[1 + signature.Length] = (byte) hashType;
            return script;
        }
    }
}