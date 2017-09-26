﻿using System.Collections.Generic;
using BitcoinUtilities;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public partial class TestScriptProcessor
    {
        [Test]
        public void TestIncompleteScript()
        {
            ScriptProcessor processor = new ScriptProcessor();
            processor.Execute(new byte[]
            {
                BitcoinScript.OP_PUSHDATA_LEN_1
            });
            Assert.False(processor.Valid);
            Assert.That(processor.GetStack(), Is.Empty);
        }

        /// <summary>
        /// Test validation of transaction generated by Bitcoin Core in regtest mode.
        /// Output of two coinbase transactions is transfered to two addresses.<para/>
        /// Coinbase 1 Private Key (WIF): cRMHqkLCb4pXs2MxpGWNDYbYTgriyfpmsSVMj4PLiA7sy8JALDLo<para/>
        /// Coinbase 2 Private Key (WIF): cQca9amk8GRzHN3fXPLEh8pNrHofyKKdoxA8GQRyuuVnpwbYqN49<para/>
        /// Output 1 Private Key (WIF): cTqvBCPk2Y8nBVgoCd1uX9cKJyDcHtWcwLenyNREdYAwUmzbWAe9<para/>
        /// Output 2 Private Key (WIF): cTEfdJ2MNqGDTLUZhySh22XNZ1LUqKEQBp5h5CQ7Vy94gJubd8bq
        /// </summary>
        [Test]
        public void Test_Spend_Coinbase_Regtest_Core()
        {
            string coinbaseTransaction1Hex =
                "02000000010000000000000000000000000000000000000000000000000000000000000000ffffffff03510101ffffffff0200f2052a010000002321022f46d46c4bc54d4142820b4da6dad9227ad6e732d3fb67c9717a7fa5e86b75a4ac0000000000000000266a24aa21a9ede2f61c3f71d1defd3fa999dfa36953755c690689799962b48bebd836974e8cf900000000";
            byte[] coinbaseTransaction1Raw;
            Assert.True(HexUtils.TryGetBytes(coinbaseTransaction1Hex, out coinbaseTransaction1Raw));
            Tx coinbaseTransaction1 = BitcoinStreamReader.FromBytes(coinbaseTransaction1Raw, Tx.Read);

            string coinbaseTransaction2Hex =
                "02000000010000000000000000000000000000000000000000000000000000000000000000ffffffff03520101ffffffff0200f2052a01000000232102b0aa0354a60481d8631083d102def3803dfe080d09a94c7feef9447a81897ffdac0000000000000000266a24aa21a9ede2f61c3f71d1defd3fa999dfa36953755c690689799962b48bebd836974e8cf900000000";
            byte[] coinbaseTransaction2Raw;
            Assert.True(HexUtils.TryGetBytes(coinbaseTransaction2Hex, out coinbaseTransaction2Raw));
            Tx coinbaseTransaction2 = BitcoinStreamReader.FromBytes(coinbaseTransaction2Raw, Tx.Read);

            Dictionary<byte[], Tx> coinbaseTransactions = new Dictionary<byte[], Tx>(ByteArrayComparer.Instance);
            coinbaseTransactions.Add(CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(coinbaseTransaction1.Write)), coinbaseTransaction1);
            coinbaseTransactions.Add(CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(coinbaseTransaction2.Write)), coinbaseTransaction2);

            string spendingTransactionHex =
                "02000000027014bdd90ea084655ab60f222a41e8f7ff132df35d9eea59344946d9bf14ed1c0000000048473044022039b640b6dccd8cedb7a5fd8967fc2bc752b440b6268fdbbc26d208079ab5948e0220339f42ac6d64689fe03aac0b04fbbf53bbffa5c3114d5336bc0bbaf4b1407f2101feffffffcd81b703b64608be8b5f0391c5d31e42df10bf3d2e702293dec7ca3972e8a2b30000000049483045022100ac63b6110185158f67af94a78ddf1fcc5e65ae4c382f448b45a7055a37b3b95a02201569acb3faca81bac7a4ebc4a2ef227a4a58785bb87618162c750915d85b425c01feffffff02606b042a010000001976a914e40e7ce54385d0c3cbf6c4adf5cace59e021cadc88ac50cd022a010000001976a914c2d6ece921ae0105fd6ac79b32ef1c685217ad2988ac7b000000";
            byte[] spendingTransactionRaw;
            Assert.True(HexUtils.TryGetBytes(spendingTransactionHex, out spendingTransactionRaw));
            Tx spendingTransaction = BitcoinStreamReader.FromBytes(spendingTransactionRaw, Tx.Read);

            BitcoinCoreSigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(spendingTransaction);

            for (int i = 0; i < spendingTransaction.Inputs.Length; i++)
            {
                TxIn input = spendingTransaction.Inputs[i];
                Tx sourceTransaction = coinbaseTransactions[input.PreviousOutput.Hash];
                TxOut sourceTransactionOutput = sourceTransaction.Outputs[input.PreviousOutput.Index];

                ScriptProcessor processor = new ScriptProcessor();
                processor.SigHashCalculator = sigHashCalculator;
                sigHashCalculator.InputIndex = i;
                processor.Execute(input.SignatureScript);
                processor.Execute(sourceTransactionOutput.PubkeyScript);
                Assert.True(processor.Valid, $"Is Valid #{i}");
                Assert.True(processor.Success, $"Is Successful #{i}");
            }
        }

        /// <summary>
        /// Test validation of transaction generated by Bitcoin Core in regtest mode.
        /// Output of one transactions with two outputs is transfered to two new addresses.<para/>
        /// Output 1 Private Key (WIF): cTqvBCPk2Y8nBVgoCd1uX9cKJyDcHtWcwLenyNREdYAwUmzbWAe9<para/>
        /// Output 2 Private Key (WIF): cTEfdJ2MNqGDTLUZhySh22XNZ1LUqKEQBp5h5CQ7Vy94gJubd8bq<para/>
        /// New Output 1 Private Key (WIF): cS8VqSdf4ZEMi9aRRZQ7Wh6ePRhkmtKsofVJiFyiari9zMWyJ74C<para/>
        /// New Output 2 Private Key (WIF): cRQsJQm3DmiCHe88X6XSzXHXGZehWQ5dNvvW5sZmEEMGkgdv6Jv2
        /// </summary>
        [Test]
        public void Test_Spend_Common_Regtest_Core()
        {
            string sourceTransactionHex =
                "02000000027014bdd90ea084655ab60f222a41e8f7ff132df35d9eea59344946d9bf14ed1c0000000048473044022039b640b6dccd8cedb7a5fd8967fc2bc752b440b6268fdbbc26d208079ab5948e0220339f42ac6d64689fe03aac0b04fbbf53bbffa5c3114d5336bc0bbaf4b1407f2101feffffffcd81b703b64608be8b5f0391c5d31e42df10bf3d2e702293dec7ca3972e8a2b30000000049483045022100ac63b6110185158f67af94a78ddf1fcc5e65ae4c382f448b45a7055a37b3b95a02201569acb3faca81bac7a4ebc4a2ef227a4a58785bb87618162c750915d85b425c01feffffff02606b042a010000001976a914e40e7ce54385d0c3cbf6c4adf5cace59e021cadc88ac50cd022a010000001976a914c2d6ece921ae0105fd6ac79b32ef1c685217ad2988ac7b000000";
            byte[] sourceTransactionRaw;
            Assert.True(HexUtils.TryGetBytes(sourceTransactionHex, out sourceTransactionRaw));
            Tx sourceTransaction = BitcoinStreamReader.FromBytes(sourceTransactionRaw, Tx.Read);

            string spendingTransactionHex =
                "020000000299d4779d924990473c54d6d84cfa7f5e74e96bc8a04248c532c39fff48b48d57000000006a4730440220066794e5442321cf75b402cefd7935eb7fdf8dcf7c56d13dd1b0c4089d90d2970220737216117c2ef9f44f5cf83fb7625e0677a4b401d6cfc21346211459326001da0121024bb6e2a5653dc68ff0ecd0ec708ad515adcacaf42fc80aba32cbb22c33f1a528feffffff99d4779d924990473c54d6d84cfa7f5e74e96bc8a04248c532c39fff48b48d57010000006a4730440220581f4f7c145dffc7a1a58ee90b3180c047b47f36d17d8e7034693e48383ee34802205d477015e2b267222af7debadd6493eb5407733708968f0bee500b6267ae975301210299b036fd0339b643b24f2b81755c6e5306cd8978ace81cfd9ebdb866d5901073feffffff02606b042a010000001976a914fd01e8cc76f126555e1321ccb5be386d0871c54188ac6018fd29010000001976a91402d08ebfcd18f0f936f728a990d6261db891835588ac86000000";
            byte[] spendingTransactionRaw;
            Assert.True(HexUtils.TryGetBytes(spendingTransactionHex, out spendingTransactionRaw));
            Tx spendingTransaction = BitcoinStreamReader.FromBytes(spendingTransactionRaw, Tx.Read);

            BitcoinCoreSigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(spendingTransaction);

            for (int i = 0; i < spendingTransaction.Inputs.Length; i++)
            {
                TxIn input = spendingTransaction.Inputs[i];
                TxOut sourceTransactionOutput = sourceTransaction.Outputs[input.PreviousOutput.Index];

                ScriptProcessor processor = new ScriptProcessor();
                processor.SigHashCalculator = sigHashCalculator;
                sigHashCalculator.InputIndex = i;
                processor.Execute(input.SignatureScript);
                processor.Execute(sourceTransactionOutput.PubkeyScript);
                Assert.True(processor.Valid, $"Is Valid #{i}");
                Assert.True(processor.Success, $"Is Successful #{i}");
            }
        }
    }
}