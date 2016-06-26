using System.Collections.Generic;

namespace BitcoinUtilities.Scripts
{
    internal class ControlStack
    {
        private readonly Stack<ControlFrame> stack = new Stack<ControlFrame>();

        public bool IsEmpty
        {
            get { return stack.Count == 0; }
        }

        public bool ExecuteBranch
        {
            get
            {
                if (stack.Count == 0)
                {
                    return true;
                }
                return stack.Peek().ExecuteBranch;
            }
        }

        public void Clear()
        {
            stack.Clear();
        }

        public void Push(bool executeBranch)
        {
            bool executeParentBranch = stack.Count == 0 || stack.Peek().ExecuteBranch;
            stack.Push(new ControlFrame(executeParentBranch && executeBranch));
        }

        public void Pop()
        {
            stack.Pop();
        }

        private struct ControlFrame
        {
            public ControlFrame(bool executeBranch)
            {
                ExecuteBranch = executeBranch;
            }

            public bool ExecuteBranch { get; }
        }
    }
}