using System;
using System.Collections.Generic;
using System.Linq;

namespace BitcoinUtilities.Scripts
{
    public class ScriptProcessor
    {
        private readonly ScriptParser parser = new ScriptParser();

        private readonly CommandDefinition[] commandDefinitions = new CommandDefinition[256];

        private readonly List<byte[]> dataStack = new List<byte[]>();
        private readonly Stack<ControlFrame> controlStack = new Stack<ControlFrame>();
        private bool valid;

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
        }

        //todo: add XMLDOC
        public bool Valid
        {
            get { return valid; }
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

            if (controlStack.Any())
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
            if (!commandDefinition.IsControlCommand && controlStack.Any() && !controlStack.Peek().ExecuteBranch)
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
            commandDefinitions[BitcoinScript.OP_ENDIF] = new CommandDefinition(true, ExecuteEndIf);
        }

        private void ExecuteIf(byte[] script, ScriptCommand command)
        {
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
            controlStack.Push(new ControlFrame(true, condition));
        }

        private void ExecuteEndIf(byte[] script, ScriptCommand command)
        {
            if (!controlStack.Any())
            {
                valid = false;
                return;
            }
            controlStack.Pop();
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

        private struct ControlFrame
        {
            public ControlFrame(bool mainBranch, bool executeBranch)
            {
                MainBranch = mainBranch;
                ExecuteBranch = executeBranch;
            }

            public bool MainBranch { get; }
            public bool ExecuteBranch { get; }
        }

        private class CommandDefinition
        {
            private readonly Action<byte[], ScriptCommand> action;

            public CommandDefinition(bool isControlCommand, Action<byte[], ScriptCommand> action)
            {
                IsControlCommand = isControlCommand;
                this.action = action;
            }

            public bool IsControlCommand { get; }

            public void Execute(byte[] script, ScriptCommand command)
            {
                action(script, command);
            }
        }
    }
}