using System;
using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public class TestControlStack
    {
        [Test]
        public void Test()
        {
            ControlStack stack = new ControlStack();

            Assert.True(stack.IsEmpty);
            Assert.True(stack.ExecuteBranch);

            stack.Push(true);
            Assert.False(stack.IsEmpty);
            Assert.True(stack.ExecuteBranch);

            stack.Push(false);
            Assert.False(stack.IsEmpty);
            Assert.False(stack.ExecuteBranch);

            stack.Push(true);
            Assert.False(stack.IsEmpty);
            Assert.False(stack.ExecuteBranch);

            stack.Pop();
            Assert.False(stack.IsEmpty);
            Assert.False(stack.ExecuteBranch);

            stack.Push(true);
            Assert.False(stack.IsEmpty);
            Assert.False(stack.ExecuteBranch);

            stack.Pop();
            Assert.False(stack.IsEmpty);
            Assert.False(stack.ExecuteBranch);

            stack.Pop();
            Assert.False(stack.IsEmpty);
            Assert.True(stack.ExecuteBranch);

            stack.Push(true);
            Assert.False(stack.IsEmpty);
            Assert.True(stack.ExecuteBranch);

            stack.Pop();
            Assert.False(stack.IsEmpty);
            Assert.True(stack.ExecuteBranch);

            stack.Pop();
            Assert.True(stack.IsEmpty);
            Assert.True(stack.ExecuteBranch);
        }

        [Test]
        public void TestClear()
        {
            ControlStack stack = new ControlStack();
            stack.Push(false);
            stack.Push(true);

            stack.Clear();
            Assert.True(stack.IsEmpty);
            Assert.True(stack.ExecuteBranch);
        }

        [Test]
        public void TestError()
        {
            ControlStack stack = new ControlStack();
            Assert.Throws<InvalidOperationException>(() => stack.Pop());
            stack.Push(false);
            stack.Pop();
            Assert.Throws<InvalidOperationException>(() => stack.Pop());
        }
    }
}