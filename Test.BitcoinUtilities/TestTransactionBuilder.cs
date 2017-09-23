﻿using BitcoinUtilities;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestTransactionBuilder
    {
        // todo: test parameters validation
        // todo: add more complex tests

        [Test]
        public void TestSimpleTransaction()
        {
            byte[] sourcePrivateKey;
            bool sourceAddressCompressed;
            Assert.True(Wif.TryDecode(BitcoinNetworkKind.Main, "KwDiBf89QgGbjEhKnhXJuH7LrciVrZi3qYjgd9M7rFU8MZejxwYf", out sourcePrivateKey, out sourceAddressCompressed));
            byte[] sourceTransactionHash = CryptoUtils.DoubleSha256(new byte[] {1, 2, 3});
            string sourceAddress = BitcoinAddress.FromPrivateKey(BitcoinNetworkKind.Main, sourcePrivateKey, sourceAddressCompressed);
            byte[] sourcePubkeyScript = BitcoinScript.CreatePayToPubkeyHash(sourceAddress);

            byte[] destPrivateKey;
            bool destAddressCompressed;
            Assert.True(Wif.TryDecode(BitcoinNetworkKind.Main, "KwDiBf89QgGbjEhKnhXJuH7LrciVrZi3qYjgd9M7rFU868E4dnmx", out destPrivateKey, out destAddressCompressed));
            string destAddress = BitcoinAddress.FromPrivateKey(BitcoinNetworkKind.Main, destPrivateKey, destAddressCompressed);
            byte[] destPubkeyScript = BitcoinScript.CreatePayToPubkeyHash(destAddress);

            TransactionBuilder builder = new TransactionBuilder();
            builder.AddInput(sourceTransactionHash, 0, sourcePubkeyScript, sourcePrivateKey, sourceAddressCompressed);
            builder.AddOutput(destPubkeyScript, 0xF123456789012345);
            Tx tx = builder.Build();

            Assert.That(tx.Inputs.Length, Is.EqualTo(1));
            Assert.That(tx.Outputs.Length, Is.EqualTo(1));

            Assert.That(tx.Outputs[0].Value, Is.EqualTo(0xF123456789012345));
            Assert.That(BitcoinScript.IsPayToPubkeyHash(tx.Outputs[0].PubkeyScript));
            Assert.That(BitcoinScript.GetAddressFromPubkeyScript(tx.Outputs[0].PubkeyScript), Is.EqualTo(destAddress));

            ScriptProcessor scriptProcessor = new ScriptProcessor();
            scriptProcessor.Transaction = tx;
            scriptProcessor.TransactionInputNumber = 0;

            scriptProcessor.Execute(tx.Inputs[0].SignatureScript);
            scriptProcessor.Execute(sourcePubkeyScript);

            Assert.True(scriptProcessor.Valid);
            Assert.True(scriptProcessor.Success);
        }
    }
}