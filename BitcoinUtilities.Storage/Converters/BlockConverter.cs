using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Storage.Models;

namespace BitcoinUtilities.Storage.Converters
{
    public class BlockConverter : IDisposable
    {
        private readonly SHA256 sha256Alg = SHA256.Create();

        public void Dispose()
        {
            sha256Alg.Dispose();
        }

        public Block FromMessage(BlockMessage blockMessage)
        {
            Block block = new Block();

            byte[] rawBlockHeader = BitcoinStreamWriter.GetBytes(blockMessage.BlockHeader.Write);
            block.Header = rawBlockHeader;
            block.Hash = sha256Alg.ComputeHash(sha256Alg.ComputeHash(rawBlockHeader));
            block.Transactions = new List<Transaction>(blockMessage.Transactions.Length);
            for (int i = 0; i < blockMessage.Transactions.Length; i++)
            {
                Tx transactionMessage = blockMessage.Transactions[i];
                Transaction transaction = FromMessage(transactionMessage);
                transaction.Block = block;
                transaction.NumberInBlock = i;
                block.Transactions.Add(transaction);
            }

            return block;
        }

        private Transaction FromMessage(Tx transactionMessage)
        {
            Transaction transaction = new Transaction();

            byte[] rawTransaction = BitcoinStreamWriter.GetBytes(transactionMessage.Write);
            transaction.Hash = sha256Alg.ComputeHash(sha256Alg.ComputeHash(rawTransaction));

            transaction.Inputs = new List<TransactionInput>(transactionMessage.Inputs.Length);
            for (int i = 0; i < transactionMessage.Inputs.Length; i++)
            {
                TxIn inputMessage = transactionMessage.Inputs[i];
                TransactionInput input = FromMessage(inputMessage);
                input.Transaction = transaction;
                input.NumberInTransaction = i;
                transaction.Inputs.Add(input);
            }

            transaction.Outputs = new List<TransactionOutput>(transactionMessage.Outputs.Length);
            for (int i = 0; i < transactionMessage.Outputs.Length; i++)
            {
                TxOut outputMessage = transactionMessage.Outputs[i];
                TransactionOutput output = FromMessage(outputMessage);
                output.Transaction = transaction;
                output.NumberInTransaction = i;
                transaction.Outputs.Add(output);
            }

            return transaction;
        }

        private TransactionInput FromMessage(TxIn inputMessage)
        {
            TransactionInput input = new TransactionInput();

            input.SignatureScript = inputMessage.SignatureScript;
            input.Sequence = inputMessage.Sequence;
            input.SourceHash = inputMessage.PreviousOutput.Hash;
            input.SourceIndex = inputMessage.PreviousOutput.Index;

            return input;
        }

        private TransactionOutput FromMessage(TxOut outputMessage)
        {
            TransactionOutput output = new TransactionOutput();

            output.Value = outputMessage.Value;
            //todo: remove or implement
            output.ScriptType = PubkeyScriptType.Plain;
            output.PubkeyScript = outputMessage.PubkeyScript;
            //todo: replace with address
            output.PublicKey = null;

            return output;
        }
    }
}