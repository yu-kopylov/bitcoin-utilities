using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Scripts
{
    public class ScriptProcessor
    {
        private readonly ScriptParser parser = new ScriptParser();

        private readonly CommandDefinition[] commandDefinitions = new CommandDefinition[256];

        private readonly List<byte[]> dataStack = new List<byte[]>();
        private readonly ControlStack controlStack = new ControlStack();

        private bool valid;
        private Tx transaction;

        public ScriptProcessor()
        {
            CreateCommandDefinitions();

            valid = true;
        }

        public void Reset()
        {
            dataStack.Clear();
            controlStack.Clear();
            valid = true;
            transaction = default(Tx);
        }

        //todo: add XMLDOC
        public bool Valid
        {
            get { return valid; }
        }

        public Tx Transaction
        {
            get { return transaction; }
            set { transaction = value; }
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
            List<ScriptCommand> commands;
            if (!parser.TryParse(script, out commands))
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

            //Flow control

            commandDefinitions[BitcoinScript.OP_NOP] = new CommandDefinition(false, (script, command) => { });
            commandDefinitions[BitcoinScript.OP_IF] = new CommandDefinition(true, ExecuteIf);
            commandDefinitions[BitcoinScript.OP_NOTIF] = new CommandDefinition(true, ExecuteIf);
            commandDefinitions[BitcoinScript.OP_ELSE] = new CommandDefinition(true, ExecuteElse);
            commandDefinitions[BitcoinScript.OP_ENDIF] = new CommandDefinition(true, ExecuteEndIf);
            commandDefinitions[BitcoinScript.OP_VERIFY] = new CommandDefinition(false, ExecuteVerify);
            commandDefinitions[BitcoinScript.OP_RETURN] = new CommandDefinition(false, ExecuteReturn);

            // Crypto

            commandDefinitions[BitcoinScript.OP_CHECKSIG] = new CommandDefinition(false, CheckSig);
        }

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
            if (!dataStack.Any())
            {
                valid = false;
                return;
                //todo: terminate execution?
            }
            //todo: add method for pop
            int index = dataStack.Count - 1;
            byte[] data = dataStack[index];
            if (!IsTrue(data))
            {
                valid = false;
            }
            else
            {
                dataStack.RemoveAt(index);
            }
        }

        private void ExecuteReturn(byte[] script, ScriptCommand command)
        {
            valid = false;
            //todo: terminate execution?
        }

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

        private void CheckSig(byte[] script, ScriptCommand command)
        {
            if (dataStack.Count < 2)
            {
                valid = false;
                return;
            }

            byte[] pubKey = dataStack[dataStack.Count - 1];
            dataStack.RemoveAt(dataStack.Count - 1);

            byte[] signatureBlock = dataStack[dataStack.Count - 1];
            dataStack.RemoveAt(dataStack.Count - 1);

            //todo: validate length first
            byte hashtype = signatureBlock[signatureBlock.Length - 1];
            byte[] signature = new byte[signatureBlock.Length - 1];
            Array.Copy(signatureBlock, 0, signature, 0, signatureBlock.Length - 1);

            //todo: process hashtype

            MemoryStream mem = new MemoryStream();

            // todo: remove signatures from script
            // todo: process OP_CODESEPARATOR
            byte[] subScript = script;

            using (BitcoinStreamWriter writer = new BitcoinStreamWriter(mem))
            {
                writer.Write(transaction.Version);
                writer.WriteCompact((ulong) transaction.Inputs.Length);
                foreach (TxIn input in transaction.Inputs)
                {
                    //input.Write(writer);
                    input.PreviousOutput.Write(writer);
                    writer.WriteCompact((ulong) subScript.Length);
                    writer.Write(subScript);
                    writer.Write(input.Sequence);
                }
                writer.WriteArray(transaction.Outputs, (w, v) => v.Write(writer));
                writer.Write(transaction.LockTime);
                writer.Write((uint) hashtype);
            }

            byte[] signedData = mem.ToArray();

            bool success = SignatureUtils.Verify(signedData, pubKey, signature);

            dataStack.Add(success ? new byte[] {1} : new byte[0]);
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