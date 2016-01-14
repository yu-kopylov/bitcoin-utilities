using System;
using System.Collections.Generic;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;
using BitcoinUtilities.Storage.Models;

namespace BitcoinUtilities.Storage.Converters
{
    public class BlockConverter
    {
        public Block FromMessage(BlockMessage blockMessage)
        {
            Block block = new Block();

            byte[] rawBlockHeader = BitcoinStreamWriter.GetBytes(blockMessage.BlockHeader.Write);
            block.Header = rawBlockHeader;
            block.Hash = CryptoUtils.DoubleSha256(rawBlockHeader);
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
            transaction.Hash = CreateTransactionHash(CryptoUtils.DoubleSha256(rawTransaction));

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

            input.SignatureScript = CreateBinaryData(inputMessage.SignatureScript);
            input.Sequence = inputMessage.Sequence;
            input.OutputHash = CreateTransactionHash(inputMessage.PreviousOutput.Hash);
            input.OutputIndex = inputMessage.PreviousOutput.Index;

            return input;
        }

        private TransactionOutput FromMessage(TxOut outputMessage)
        {
            TransactionOutput output = new TransactionOutput();

            output.Value = outputMessage.Value;
            output.PubkeyScript = CreateBinaryData(outputMessage.PubkeyScript);
            output.Address = CreateAddress(outputMessage.PubkeyScript);

            return output;
        }

        private Address CreateAddress(byte[] pubkeyScript)
        {
            string address = BitcoinScript.GetAddressFromPubkeyScript(pubkeyScript);

            if (address == null)
            {
                return null;
            }

            //todo: use a more well-known hash-function
            uint semiHash = 23;
            foreach (char c in address)
            {
                semiHash = semiHash*31 + (byte)c;
            }

            Address res = new Address();
            res.Value = address;
            res.SemiHash = semiHash;
            return res;
        }

        private static BinaryData CreateBinaryData(byte[] data)
        {
            BinaryData res = new BinaryData();
            res.Data = data;
            return res;
        }

        public static TransactionHash CreateTransactionHash(byte[] hash)
        {
            TransactionHash res = new TransactionHash();

            res.Hash = hash;
            res.SemiHash = BitConverter.ToUInt32(hash, 0);

            return res;
        }
    }
}