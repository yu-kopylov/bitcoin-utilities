using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BitcoinUtilities.Scripts
{
    public class ScriptProcessor
    {
        // todo: convert ScriptParser to a static class ?
        private readonly ScriptParser parser = new ScriptParser();

        private readonly CommandDefinition[] commandDefinitions = new CommandDefinition[256];

        private readonly List<byte[]> dataStack = new List<byte[]>();
        private readonly Stack<byte[]> altDataStack = new Stack<byte[]>();
        private readonly ControlStack controlStack = new ControlStack();

        private ISigHashCalculator sigHashCalculator;

        private bool valid;
        private int lastCodeSeparator = -1;

        public ScriptProcessor()
        {
            // todo: move to a static constructor?
            CreateCommandDefinitions();

            valid = true;
        }

        public void Reset()
        {
            // todo: add xml doc, describe SigHashCalculator usage
            dataStack.Clear();
            altDataStack.Clear();
            controlStack.Clear();
            valid = true;
            sigHashCalculator = null;
            lastCodeSeparator = -1;
        }

        //todo: add XMLDOC
        public bool Valid
        {
            get { return valid; }
        }

        public bool Success
        {
            //todo: choose better name and write tests
            get
            {
                if (dataStack.Count == 0)
                {
                    return false;
                }

                return IsTrue(dataStack[dataStack.Count - 1]);
            }
        }

        public ISigHashCalculator SigHashCalculator
        {
            get { return sigHashCalculator; }
            set { sigHashCalculator = value; }
        }

        /// <summary>
        /// Returns the current contents of the stack as a list.
        /// Top items of the stack are placed in the beginning of the list. 
        /// </summary>
        public List<byte[]> GetStack()
        {
            List<byte[]> res = dataStack.ToList();
            res.Reverse();
            return res;
        }

        public void Execute(byte[] script)
        {
            if (!parser.TryParse(script, out var commands))
            {
                valid = false;
                return;
            }

            foreach (ScriptCommand command in commands)
            {
                Execute(script, command);
            }

            if (!controlStack.IsEmpty)
            {
                valid = false;
            }
        }

        private void Execute(byte[] script, ScriptCommand command)
        {
            CommandDefinition commandDefinition = commandDefinitions[command.Code];
            if (commandDefinition == null)
            {
                //todo: make sure that this exception is never thrown
                throw new InvalidOperationException($"Command '0x{command.Code:X2}' is not supported.");
            }

            if (!commandDefinition.IsControlCommand && !controlStack.ExecuteBranch)
            {
                return;
            }

            commandDefinition.Execute(script, command);
        }

        private void CreateCommandDefinitions()
        {
            CommandDefinition disabled = new CommandDefinition(true, (_, __) => { valid = false; });

            // Constants

            commandDefinitions[BitcoinScript.OP_FALSE] = new CommandDefinition(false, (script, command) => dataStack.Add(new byte[0]));
            for (byte op = BitcoinScript.OP_PUSHDATA_LEN_1; op <= BitcoinScript.OP_PUSHDATA_LEN_75; op++)
            {
                commandDefinitions[op] = new CommandDefinition(false, PushData);
            }

            commandDefinitions[BitcoinScript.OP_PUSHDATA1] = new CommandDefinition(false, PushData1);
            commandDefinitions[BitcoinScript.OP_PUSHDATA2] = new CommandDefinition(false, PushData2);
            commandDefinitions[BitcoinScript.OP_PUSHDATA4] = new CommandDefinition(false, PushData4);
            commandDefinitions[BitcoinScript.OP_1NEGATE] = new CommandDefinition(false, (script, command) => dataStack.Add(new byte[] {0x81}));
            commandDefinitions[BitcoinScript.OP_TRUE] = new CommandDefinition(false, (script, command) => dataStack.Add(new byte[] {1}));
            for (byte op = BitcoinScript.OP_2; op <= BitcoinScript.OP_16; op++)
            {
                byte val = (byte) (op - BitcoinScript.OP_2 + 2);
                commandDefinitions[op] = new CommandDefinition(false, (script, command) => dataStack.Add(new byte[] {val}));
            }

            // Flow control

            commandDefinitions[BitcoinScript.OP_NOP] = new CommandDefinition(false, (script, command) => { });
            commandDefinitions[BitcoinScript.OP_IF] = new CommandDefinition(true, ExecuteIf);
            commandDefinitions[BitcoinScript.OP_NOTIF] = new CommandDefinition(true, ExecuteIf);
            commandDefinitions[BitcoinScript.OP_ELSE] = new CommandDefinition(true, ExecuteElse);
            commandDefinitions[BitcoinScript.OP_ENDIF] = new CommandDefinition(true, ExecuteEndIf);
            commandDefinitions[BitcoinScript.OP_VERIFY] = new CommandDefinition(false, ExecuteVerify);
            commandDefinitions[BitcoinScript.OP_RETURN] = new CommandDefinition(false, ExecuteReturn);

            // Stack

            commandDefinitions[BitcoinScript.OP_TOALTSTACK] = new CommandDefinition(false, ExecuteToAltStack);
            commandDefinitions[BitcoinScript.OP_FROMALTSTACK] = new CommandDefinition(false, ExecuteFromAltStack);
            commandDefinitions[BitcoinScript.OP_2DROP] = new CommandDefinition(false, Execute2Drop);
            commandDefinitions[BitcoinScript.OP_2DUP] = new CommandDefinition(false, Execute2Dup);
            commandDefinitions[BitcoinScript.OP_3DUP] = new CommandDefinition(false, Execute3Dup);
            commandDefinitions[BitcoinScript.OP_2OVER] = new CommandDefinition(false, Execute2Over);
            commandDefinitions[BitcoinScript.OP_2ROT] = new CommandDefinition(false, Execute2Rot);
            commandDefinitions[BitcoinScript.OP_2SWAP] = new CommandDefinition(false, Execute2Swap);
            commandDefinitions[BitcoinScript.OP_IFDUP] = new CommandDefinition(false, ExecuteIfDup);
            commandDefinitions[BitcoinScript.OP_DEPTH] = new CommandDefinition(false, ExecuteDepth);
            commandDefinitions[BitcoinScript.OP_DROP] = new CommandDefinition(false, ExecuteDrop);
            commandDefinitions[BitcoinScript.OP_DUP] = new CommandDefinition(false, ExecuteDup);
            commandDefinitions[BitcoinScript.OP_NIP] = new CommandDefinition(false, ExecuteNip);
            commandDefinitions[BitcoinScript.OP_OVER] = new CommandDefinition(false, ExecuteOver);
            commandDefinitions[BitcoinScript.OP_PICK] = new CommandDefinition(false, ExecutePick);
            commandDefinitions[BitcoinScript.OP_ROLL] = new CommandDefinition(false, ExecuteRoll);
            commandDefinitions[BitcoinScript.OP_ROT] = new CommandDefinition(false, ExecuteRot);
            commandDefinitions[BitcoinScript.OP_SWAP] = new CommandDefinition(false, ExecuteSwap);
            commandDefinitions[BitcoinScript.OP_TUCK] = new CommandDefinition(false, ExecuteTuck);

            // Splice

            commandDefinitions[BitcoinScript.OP_CAT] = disabled;
            commandDefinitions[BitcoinScript.OP_SUBSTR] = disabled;
            commandDefinitions[BitcoinScript.OP_LEFT] = disabled;
            commandDefinitions[BitcoinScript.OP_RIGHT] = disabled;
            commandDefinitions[BitcoinScript.OP_SIZE] = new CommandDefinition(false, ExecuteSize);

            // Bitwise Logic

            commandDefinitions[BitcoinScript.OP_INVERT] = disabled;
            commandDefinitions[BitcoinScript.OP_AND] = disabled;
            commandDefinitions[BitcoinScript.OP_OR] = disabled;
            commandDefinitions[BitcoinScript.OP_XOR] = disabled;
            commandDefinitions[BitcoinScript.OP_EQUAL] = new CommandDefinition(false, ExecuteEqual);
            commandDefinitions[BitcoinScript.OP_EQUALVERIFY] = new CommandDefinition(false, ExecuteEqualVerify);

            // Arithmetic

            commandDefinitions[BitcoinScript.OP_1ADD] = new CommandDefinition(false, ExecuteAdd1);
            commandDefinitions[BitcoinScript.OP_1SUB] = new CommandDefinition(false, ExecuteSubtract1);
            commandDefinitions[BitcoinScript.OP_2MUL] = disabled;
            commandDefinitions[BitcoinScript.OP_2DIV] = disabled;
            commandDefinitions[BitcoinScript.OP_NEGATE] = new CommandDefinition(false, ExecuteNegate);
            commandDefinitions[BitcoinScript.OP_ABS] = new CommandDefinition(false, ExecuteAbs);
            commandDefinitions[BitcoinScript.OP_NOT] = new CommandDefinition(false, ExecuteNot);
            commandDefinitions[BitcoinScript.OP_ADD] = new CommandDefinition(false, ExecuteAdd);
            commandDefinitions[BitcoinScript.OP_SUB] = new CommandDefinition(false, ExecuteSubtract);
            commandDefinitions[BitcoinScript.OP_MUL] = disabled;
            commandDefinitions[BitcoinScript.OP_DIV] = disabled;
            commandDefinitions[BitcoinScript.OP_MOD] = disabled;
            commandDefinitions[BitcoinScript.OP_LSHIFT] = disabled;
            commandDefinitions[BitcoinScript.OP_RSHIFT] = disabled;
            commandDefinitions[BitcoinScript.OP_MIN] = new CommandDefinition(false, ExecuteMin);
            commandDefinitions[BitcoinScript.OP_MAX] = new CommandDefinition(false, ExecuteMax);

            // Crypto

            commandDefinitions[BitcoinScript.OP_RIPEMD160] = new CommandDefinition(false, ExecuteRipeMd160);
            commandDefinitions[BitcoinScript.OP_SHA1] = new CommandDefinition(false, ExecuteSha1);
            commandDefinitions[BitcoinScript.OP_SHA256] = new CommandDefinition(false, ExecuteSha256);
            commandDefinitions[BitcoinScript.OP_HASH160] = new CommandDefinition(false, ExecuteHash160);
            commandDefinitions[BitcoinScript.OP_HASH256] = new CommandDefinition(false, ExecuteHash256);
            commandDefinitions[BitcoinScript.OP_CODESEPARATOR] = new CommandDefinition(false, ExecuteCodeSeparator);
            commandDefinitions[BitcoinScript.OP_CHECKSIG] = new CommandDefinition(false, ExecuteCheckSig);
            commandDefinitions[BitcoinScript.OP_CHECKSIGVERIFY] = new CommandDefinition(false, ExecuteCheckSigVerify);
            commandDefinitions[BitcoinScript.OP_CHECKMULTISIG] = new CommandDefinition(false, ExecuteCheckMultiSig);
            commandDefinitions[BitcoinScript.OP_CHECKMULTISIGVERIFY] = new CommandDefinition(false, ExecuteCheckMultiSigVerify);

            // Reserved words

            commandDefinitions[BitcoinScript.OP_VERIF] = disabled;
            commandDefinitions[BitcoinScript.OP_VERNOTIF] = disabled;

            commandDefinitions[BitcoinScript.OP_RESERVED] = new CommandDefinition(false, (_, __) => { valid = false; });
            commandDefinitions[BitcoinScript.OP_VER] = new CommandDefinition(false, (_, __) => { valid = false; });
            commandDefinitions[BitcoinScript.OP_RESERVED1] = new CommandDefinition(false, (_, __) => { valid = false; });
            commandDefinitions[BitcoinScript.OP_RESERVED2] = new CommandDefinition(false, (_, __) => { valid = false; });

            commandDefinitions[BitcoinScript.OP_NOP1] = new CommandDefinition(false, (_, __) => { });
            commandDefinitions[BitcoinScript.OP_NOP4] = new CommandDefinition(false, (_, __) => { });
            commandDefinitions[BitcoinScript.OP_NOP5] = new CommandDefinition(false, (_, __) => { });
            commandDefinitions[BitcoinScript.OP_NOP6] = new CommandDefinition(false, (_, __) => { });
            commandDefinitions[BitcoinScript.OP_NOP7] = new CommandDefinition(false, (_, __) => { });
            commandDefinitions[BitcoinScript.OP_NOP8] = new CommandDefinition(false, (_, __) => { });
            commandDefinitions[BitcoinScript.OP_NOP9] = new CommandDefinition(false, (_, __) => { });
            commandDefinitions[BitcoinScript.OP_NOP10] = new CommandDefinition(false, (_, __) => { });
        }

        #region Constants Commands

        private void PushData(byte[] script, ScriptCommand command)
        {
            byte[] data = new byte[command.Code];
            Array.Copy(script, command.Offset + 1, data, 0, command.Code);
            dataStack.Add(data);
        }

        private void PushData1(byte[] script, ScriptCommand command)
        {
            int dataLength = command.Length - 2;
            byte[] data = new byte[dataLength];
            Array.Copy(script, command.Offset + 2, data, 0, dataLength);
            dataStack.Add(data);
        }

        private void PushData2(byte[] script, ScriptCommand command)
        {
            int dataLength = command.Length - 3;
            byte[] data = new byte[dataLength];
            Array.Copy(script, command.Offset + 3, data, 0, dataLength);
            dataStack.Add(data);
        }

        private void PushData4(byte[] script, ScriptCommand command)
        {
            int dataLength = command.Length - 5;
            byte[] data = new byte[dataLength];
            Array.Copy(script, command.Offset + 5, data, 0, dataLength);
            dataStack.Add(data);
        }

        #endregion

        #region Flow Control Commands

        private void ExecuteIf(byte[] script, ScriptCommand command)
        {
            if (!controlStack.ExecuteBranch)
            {
                controlStack.Push(false);
                return;
            }

            bool condition;
            if (!dataStack.Any())
            {
                condition = false;
                valid = false;
                //todo: terminate execution?
            }
            else
            {
                int index = dataStack.Count - 1;
                byte[] data = dataStack[index];
                dataStack.RemoveAt(index);
                condition = IsTrue(data);
            }

            if (command.Code == BitcoinScript.OP_NOTIF)
            {
                condition = !condition;
            }

            controlStack.Push(condition);
        }

        private void ExecuteElse(byte[] script, ScriptCommand command)
        {
            if (controlStack.IsEmpty)
            {
                valid = false;
                return;
            }

            bool executeBranch = !controlStack.ExecuteBranch;
            controlStack.Pop();
            controlStack.Push(executeBranch);
        }

        private void ExecuteEndIf(byte[] script, ScriptCommand command)
        {
            if (controlStack.IsEmpty)
            {
                valid = false;
                return;
            }

            controlStack.Pop();
        }

        private void ExecuteVerify(byte[] script, ScriptCommand command)
        {
            byte[] data = PopData();
            if (data == null)
            {
                valid = false;
                return;
            }

            if (!IsTrue(data))
            {
                valid = false;
                dataStack.Add(data);
            }
        }

        private void ExecuteReturn(byte[] script, ScriptCommand command)
        {
            valid = false;
            //todo: terminate execution?
        }

        #endregion

        #region Stack Commands

        private void ExecuteToAltStack(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            byte[] data = PopData();
            if (data == null)
            {
                valid = false;
                return;
            }

            altDataStack.Push(data);
        }

        private void ExecuteFromAltStack(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (altDataStack.Count == 0)
            {
                valid = false;
                return;
            }

            byte[] data = altDataStack.Pop();
            dataStack.Add(data);
        }

        private void Execute2Drop(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            PopData();
            PopData();
        }

        private void Execute2Dup(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item1 = dataStack[dataStack.Count - 1];
            byte[] item2 = dataStack[dataStack.Count - 2];
            dataStack.Add(item2);
            dataStack.Add(item1);
        }

        private void Execute3Dup(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 3)
            {
                valid = false;
                return;
            }

            byte[] item1 = dataStack[dataStack.Count - 1];
            byte[] item2 = dataStack[dataStack.Count - 2];
            byte[] item3 = dataStack[dataStack.Count - 3];
            dataStack.Add(item3);
            dataStack.Add(item2);
            dataStack.Add(item1);
        }

        private void Execute2Over(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 4)
            {
                valid = false;
                return;
            }

            byte[] item1 = dataStack[dataStack.Count - 3];
            byte[] item2 = dataStack[dataStack.Count - 4];
            dataStack.Add(item2);
            dataStack.Add(item1);
        }

        private void Execute2Rot(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 6)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();
            byte[] item3 = PopData();
            byte[] item4 = PopData();
            byte[] item5 = PopData();
            byte[] item6 = PopData();

            dataStack.Add(item4);
            dataStack.Add(item3);
            dataStack.Add(item2);
            dataStack.Add(item1);

            dataStack.Add(item6);
            dataStack.Add(item5);
        }

        private void Execute2Swap(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 4)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();
            byte[] item3 = PopData();
            byte[] item4 = PopData();

            dataStack.Add(item2);
            dataStack.Add(item1);

            dataStack.Add(item4);
            dataStack.Add(item3);
        }

        private void ExecuteIfDup(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            byte[] item = PopData();

            if (item == null)
            {
                valid = false;
                return;
            }

            dataStack.Add(item);

            if (IsTrue(item))
            {
                dataStack.Add(item);
            }
        }

        private void ExecuteDepth(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            BigInteger val = dataStack.Count;
            byte[] item = val.ToByteArray();
            dataStack.Add(item);
        }

        private void ExecuteDrop(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            byte[] item = PopData();
            if (item == null)
            {
                valid = false;
            }
        }

        private void ExecuteDup(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            byte[] item = PopData();
            if (item == null)
            {
                valid = false;
                return;
            }

            dataStack.Add(item);
            dataStack.Add(item);
        }

        private void ExecuteNip(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            dataStack.RemoveAt(dataStack.Count - 2);
        }

        private void ExecuteOver(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item = dataStack[dataStack.Count - 2];
            dataStack.Add(item);
        }

        private void ExecutePick(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] numAsBytes = PopData();
            BigInteger numAsBigInt = new BigInteger(numAsBytes);
            if (numAsBigInt < 0 || numAsBigInt >= dataStack.Count)
            {
                valid = false;
                return;
            }

            // We assume that dataStack.Count is always less then int.MaxValue. Therefore, this conversion should never fail.
            int num = (int) numAsBigInt;
            byte[] item = dataStack[dataStack.Count - num];
            dataStack.Add(item);
        }

        private void ExecuteRoll(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] numAsBytes = PopData();
            BigInteger numAsBigInt = new BigInteger(numAsBytes);
            if (numAsBigInt < 0 || numAsBigInt >= dataStack.Count)
            {
                valid = false;
                return;
            }

            // We assume that dataStack.Count is always less then int.MaxValue. Therefore, this conversion should never fail.
            int num = (int) numAsBigInt;
            byte[] item = dataStack[dataStack.Count - num];
            dataStack.RemoveAt(dataStack.Count - num);
            dataStack.Add(item);
        }

        private void ExecuteRot(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 3)
            {
                valid = false;
                return;
            }

            byte[] item3 = dataStack[dataStack.Count - 3];
            dataStack.RemoveAt(dataStack.Count - 3);
            dataStack.Add(item3);
        }

        private void ExecuteSwap(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();

            dataStack.Add(item1);
            dataStack.Add(item2);
        }

        private void ExecuteTuck(byte[] script, ScriptCommand command)
        {
            //todo: add tests
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item = dataStack[dataStack.Count - 1];
            dataStack.Insert(dataStack.Count - 2, item);
        }

        #endregion

        #region Splice

        private void ExecuteSize(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 1)
            {
                valid = false;
                return;
            }

            int size = dataStack[dataStack.Count - 1].Length;
            dataStack.Add(EncodeInt(size));
        }

        #endregion

        #region Bitwise Logic Commands

        private void ExecuteEqual(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();

            bool equal = item1.SequenceEqual(item2);
            dataStack.Add(equal ? new byte[] {1} : new byte[0]);
        }

        private void ExecuteEqualVerify(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();

            bool equal = item1.SequenceEqual(item2);

            if (!equal)
            {
                valid = false;
                dataStack.Add(new byte[0]);
            }
        }

        #endregion

        #region Arithmetic Commands

        private void ExecuteAdd1(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 1)
            {
                valid = false;
                return;
            }

            byte[] item = PopData();

            if (!ParseInt32(item, out long value))
            {
                valid = false;
                return;
            }

            dataStack.Add(EncodeInt(value + 1));
        }

        private void ExecuteSubtract1(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 1)
            {
                valid = false;
                return;
            }

            byte[] item = PopData();

            if (!ParseInt32(item, out long value))
            {
                valid = false;
                return;
            }

            dataStack.Add(EncodeInt(value - 1));
        }

        private void ExecuteNegate(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 1)
            {
                valid = false;
                return;
            }

            byte[] item = PopData();

            if (!ParseInt32(item, out long value))
            {
                valid = false;
                return;
            }

            dataStack.Add(EncodeInt(-value));
        }

        private void ExecuteAbs(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 1)
            {
                valid = false;
                return;
            }

            byte[] item = PopData();

            if (!ParseInt32(item, out long value))
            {
                valid = false;
                return;
            }

            dataStack.Add(EncodeInt(value < 0 ? -value : value));
        }

        private void ExecuteNot(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 1)
            {
                valid = false;
                return;
            }

            byte[] item = PopData();

            if (!ParseInt32(item, out long value))
            {
                valid = false;
                return;
            }

            if (value == 0)
            {
                dataStack.Add(new byte[] {1});
            }
            else
            {
                dataStack.Add(new byte[0]);
            }
        }

        private void ExecuteAdd(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();

            if (!ParseInt32(item2, out long a) || !ParseInt32(item1, out long b))
            {
                valid = false;
                return;
            }

            dataStack.Add(EncodeInt(a + b));
        }

        private void ExecuteSubtract(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();

            if (!ParseInt32(item2, out long a) || !ParseInt32(item1, out long b))
            {
                valid = false;
                return;
            }

            dataStack.Add(EncodeInt(a - b));
        }

        private void ExecuteMin(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();

            if (!ParseInt32(item2, out long a) || !ParseInt32(item1, out long b))
            {
                valid = false;
                return;
            }

            dataStack.Add(EncodeInt(a < b ? a : b));
        }

        private void ExecuteMax(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] item1 = PopData();
            byte[] item2 = PopData();

            if (!ParseInt32(item2, out long a) || !ParseInt32(item1, out long b))
            {
                valid = false;
                return;
            }

            dataStack.Add(EncodeInt(a > b ? a : b));
        }

        private bool ParseInt32(byte[] encoded, out long value)
        {
            if (encoded.Length == 0)
            {
                value = 0;
                return true;
            }

            if (encoded.Length > 4)
            {
                value = 0;
                return false;
            }

            int lastIndex = encoded.Length - 1;

            // Checking that encoding is minimal.
            if ((encoded[lastIndex] & 0x7F) == 0)
            {
                // Last byte does not add value except for a sign. It is required only if previous byte used its highest bit for value.
                // If there is no previous byte, then encoded value represents negative zero, which should have been represented as zero instead.
                bool extraByteRequired = lastIndex > 0 && encoded[lastIndex - 1] >= 0x80;
                if (!extraByteRequired)
                {
                    value = 0;
                    return false;
                }
            }

            // reading sign bit from the last byte
            bool isNegative = encoded[lastIndex] >= 0x80;

            long res = 0;
            for (int i = 0; i < lastIndex; i++)
            {
                res |= ((long) encoded[i]) << (8 * i);
            }

            // excluding sign bit from the last byte
            res |= ((long) (encoded[lastIndex] & 0x7F)) << (8 * lastIndex);

            if (isNegative)
            {
                res = -res;
            }

            value = res;
            return true;
        }

        private byte[] EncodeInt(long value)
        {
            if (value == 0)
            {
                return new byte[0];
            }

            List<byte> encoded = new List<byte>();

            bool isNegative = value < 0;
            long absValue = isNegative ? -value : value;

            while (absValue != 0)
            {
                encoded.Add((byte) absValue);
                absValue >>= 8;
            }

            int lastIndex = encoded.Count - 1;

            if (encoded[lastIndex] >= 0x80)
            {
                encoded.Add(isNegative ? (byte) 0x80 : (byte) 0x00);
            }
            else if (isNegative)
            {
                encoded[lastIndex] |= 0x80;
            }

            return encoded.ToArray();
        }

        #endregion

        #region Crypto Commands

        private void ExecuteRipeMd160(byte[] script, ScriptCommand command)
        {
            byte[] data = PopData();
            if (data == null)
            {
                valid = false;
                return;
            }

            dataStack.Add(CryptoUtils.RipeMd160(data));
        }

        private void ExecuteSha1(byte[] script, ScriptCommand command)
        {
            byte[] data = PopData();
            if (data == null)
            {
                valid = false;
                return;
            }

            dataStack.Add(CryptoUtils.Sha1(data));
        }

        private void ExecuteSha256(byte[] script, ScriptCommand command)
        {
            byte[] data = PopData();
            if (data == null)
            {
                valid = false;
                return;
            }

            dataStack.Add(CryptoUtils.Sha256(data));
        }

        private void ExecuteHash160(byte[] script, ScriptCommand command)
        {
            byte[] data = PopData();
            if (data == null)
            {
                valid = false;
                return;
            }

            dataStack.Add(CryptoUtils.RipeMd160(CryptoUtils.Sha256(data)));
        }

        private void ExecuteHash256(byte[] script, ScriptCommand command)
        {
            byte[] data = PopData();
            if (data == null)
            {
                valid = false;
                return;
            }

            dataStack.Add(CryptoUtils.DoubleSha256(data));
        }

        private void ExecuteCodeSeparator(byte[] script, ScriptCommand command)
        {
            // todo: add tests
            lastCodeSeparator = command.Offset;
        }

        private void ExecuteCheckSig(byte[] script, ScriptCommand command)
        {
            bool success = CheckSig(script);
            dataStack.Add(success ? new byte[] {1} : new byte[0]);
        }

        private void ExecuteCheckSigVerify(byte[] script, ScriptCommand command)
        {
            //todo: add test
            bool success = CheckSig(script);
            if (!success)
            {
                valid = false;
                dataStack.Add(new byte[0]);
            }
        }

        private void ExecuteCheckMultiSig(byte[] script, ScriptCommand command)
        {
            bool success = CheckMultiSig(script);
            dataStack.Add(success ? new byte[] {1} : new byte[0]);
        }

        private void ExecuteCheckMultiSigVerify(byte[] script, ScriptCommand command)
        {
            throw new NotImplementedException("OP_CHECKMULTISIGVERIFY is not implemented yet.");
        }

        private bool CheckSig(byte[] script)
        {
            if (dataStack.Count < 2)
            {
                valid = false;
                return false;
            }

            byte[] pubKey = PopData();
            byte[] signatureBlock = PopData();

            if (!ParseSignatureBlock(signatureBlock, out byte[] signature, out var hashType))
            {
                valid = false;
                return false;
            }

            byte[] subScript = ExtractSubScript(script);

            if (sigHashCalculator == null)
            {
                throw new InvalidOperationException($"{nameof(SigHashCalculator)} is required for signature verification.");
            }

            byte[] signedData = sigHashCalculator.Calculate(hashType, subScript);

            return SignatureUtils.Verify(signedData, pubKey, signature);
        }

        private bool CheckMultiSig(byte[] script)
        {
            if (dataStack.Count < 5)
            {
                valid = false;
                return false;
            }

            byte[] publicKeyCountEncoded = PopData();
            if (publicKeyCountEncoded.Length != 1)
            {
                valid = false;
                return false;
            }

            // todo: is it correct to decode number this way?
            int publicKeyCount = publicKeyCountEncoded[0];
            if (publicKeyCount <= 0 || publicKeyCount >= dataStack.Count || publicKeyCount >= 0x7F)
            {
                valid = false;
                return false;
            }

            byte[][] publicKeys = new byte[publicKeyCount][];
            for (int i = 0; i < publicKeyCount; i++)
            {
                publicKeys[i] = PopData();
            }

            byte[] signatureCountEncoded = PopData();
            if (signatureCountEncoded.Length != 1)
            {
                valid = false;
                return false;
            }

            // todo: is it correct to decode number this way?
            int signaturesCount = signatureCountEncoded[0];
            if (signaturesCount <= 0 || signaturesCount >= dataStack.Count || signaturesCount >= 0x7F)
            {
                valid = false;
                return false;
            }

            byte[][] signatures = new byte[signaturesCount][];
            SigHashType[] hashTypes = new SigHashType[signaturesCount];
            for (int i = 0; i < signaturesCount; i++)
            {
                byte[] signatureBlock = PopData();
                if (!ParseSignatureBlock(signatureBlock, out signatures[i], out hashTypes[i]))
                {
                    valid = false;
                    return false;
                }
            }

            // one extra unused value
            PopData();

            byte[] subScript = ExtractSubScript(script);

            if (sigHashCalculator == null)
            {
                throw new InvalidOperationException($"{nameof(SigHashCalculator)} is required for signature verification.");
            }

            int signatureIndex = 0;
            SigHashType hashType = hashTypes[0];
            byte[] signedData = sigHashCalculator.Calculate(hashType, subScript);

            for (int publicKeyIndex = 0; publicKeyIndex < publicKeyCount && signatureIndex < signaturesCount; publicKeyIndex++)
            {
                if (hashTypes[signatureIndex] != hashType)
                {
                    hashType = hashTypes[signatureIndex];
                    signedData = sigHashCalculator.Calculate(hashType, subScript);
                }

                if (SignatureUtils.Verify(signedData, publicKeys[publicKeyIndex], signatures[signatureIndex]))
                {
                    signatureIndex++;
                }
            }

            return signatureIndex == signaturesCount;
        }

        private byte[] ExtractSubScript(byte[] script)
        {
            // todo: also remove all OP_CODESEPARATORs from script ?
            byte[] subScript = script;
            if (lastCodeSeparator >= 0)
            {
                subScript = new byte[script.Length - lastCodeSeparator - 1];
                Array.Copy(script, lastCodeSeparator + 1, subScript, 0, script.Length - lastCodeSeparator - 1);
            }

            return subScript;
        }

        private static bool ParseSignatureBlock(byte[] signatureBlock, out byte[] signature, out SigHashType hashType)
        {
            if (signatureBlock.Length == 0)
            {
                signature = null;
                hashType = 0;
                return false;
            }

            hashType = (SigHashType) signatureBlock[signatureBlock.Length - 1];
            signature = new byte[signatureBlock.Length - 1];
            Array.Copy(signatureBlock, 0, signature, 0, signatureBlock.Length - 1);

            return true;
        }

        #endregion

        private byte[] PopData()
        {
            if (dataStack.Count == 0)
            {
                // todo: throw exception instead?
                return null;
            }

            int index = dataStack.Count - 1;
            byte[] res = dataStack[index];
            dataStack.RemoveAt(index);
            return res;
        }

        private bool IsTrue(byte[] data)
        {
            if (data.Length == 0)
            {
                return false;
            }

            int lastIndex = data.Length - 1;
            if (data[lastIndex] != 0 && data[lastIndex] != 0x80)
            {
                return true;
            }

            for (int i = 0; i < lastIndex; i++)
            {
                if (data[i] != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private class CommandDefinition
        {
            private readonly Action<byte[], ScriptCommand> action;

            public CommandDefinition(bool isControlCommand, Action<byte[], ScriptCommand> action)
            {
                IsControlCommand = isControlCommand;
                this.action = action;
            }

            /// <summary>
            /// Indicates that command should be executed even when occuring in an unexecuted OP_IF branch.
            /// </summary>
            public bool IsControlCommand { get; }

            public void Execute(byte[] script, ScriptCommand command)
            {
                action(script, command);
            }
        }
    }
}