﻿using System;
using System.Collections.Generic;

namespace BitcoinUtilities.Storage
{
    /// <summary>
    /// This class allows to perform operations on the UTXO set avoiding changes to the underlying unstorage.
    /// </summary>
    public class UnspentOutputsUpdate
    {
        private readonly IUnspentOutputStorage storage;

        private readonly Dictionary<byte[], TransactionState> transactions = new Dictionary<byte[], TransactionState>(ByteArrayComparer.Instance);

        public UnspentOutputsUpdate(IUnspentOutputStorage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Returns all unspent outputs of a transaction with the given hash.
        /// </summary>
        public List<UnspentOutput> FindUnspentOutputs(byte[] transactionHash)
        {
            TransactionState transactionState = LoadTransaction(transactionHash);
            List<UnspentOutput> res = new List<UnspentOutput>();
            foreach (OutputState outputState in transactionState.Outputs.Values)
            {
                UnspentOutput output = outputState.CurrentOutput;
                if (output != null)
                {
                    res.Add(output);
                }
            }
            return res;
        }

        /// <summary>
        /// Returns the unspent output with the given number of a transaction with the given hash.
        /// </summary>
        /// <returns>The unspent output; or null if does not exist.</returns>
        public UnspentOutput FindUnspentOutput(byte[] transactionHash, int outputNumber)
        {
            TransactionState transactionState = LoadTransaction(transactionHash);

            OutputState outputState;
            if (!transactionState.Outputs.TryGetValue(outputNumber, out outputState))
            {
                return null;
            }

            return outputState.CurrentOutput;
        }

        /// <summary>
        /// Adds the unspent output. 
        /// </summary>
        /// <exception cref="InvalidOperationException">If an output with same number of the same transaction already exists.</exception>
        public void Add(UnspentOutput unspentOutput)
        {
            TransactionState transactionState = LoadTransaction(unspentOutput.TransactionHash);
            OutputState outputState;
            if (!transactionState.Outputs.TryGetValue(unspentOutput.OutputNumber, out outputState))
            {
                outputState = new OutputState();
                transactionState.Outputs.Add(unspentOutput.OutputNumber, outputState);
            }
            if (outputState.CurrentOutput != null)
            {
                throw new InvalidOperationException(
                    $"Transaction '{BitConverter.ToString(unspentOutput.TransactionHash)}' already has output with number {unspentOutput.OutputNumber}.");
            }
            outputState.NewOutput = unspentOutput;
        }

        /// <summary>
        /// Spends the output with the given number of a transaction with the given hash.
        /// </summary>
        /// <param name="transactionHash">The hash of a transaction.</param>
        /// <param name="outputNumber">The number of the output.</param>
        /// <param name="block">The block in which output was spent.</param>
        /// <exception cref="InvalidOperationException">If the output with given parameters does not exist or was already spent.</exception>
        public void Spend(byte[] transactionHash, int outputNumber, StoredBlock block)
        {
            TransactionState transactionState = LoadTransaction(transactionHash);
            OutputState outputState;
            if (!transactionState.Outputs.TryGetValue(outputNumber, out outputState))
            {
                throw new InvalidOperationException($"Transaction '{BitConverter.ToString(transactionHash)}' does not have output with number {outputNumber}.");
            }

            if (outputState.NewOutput != null)
            {
                outputState.NewOutput = null;
            }
            else if (outputState.StoredOutput != null && outputState.SpentOutput == null)
            {
                outputState.SpentOutput = SpentOutput.Create(block, outputState.StoredOutput);
            }
            else
            {
                throw new InvalidOperationException($"Transaction '{BitConverter.ToString(transactionHash)}' does not have output with number {outputNumber}.");
            }
        }

        /// <summary>
        /// Saves changes to the underlying storage.
        /// </summary>
        public void Persist()
        {
            foreach (TransactionState transactionState in transactions.Values)
            {
                foreach (OutputState outputState in transactionState.Outputs.Values)
                {
                    SpentOutput spentOutput = outputState.SpentOutput;
                    if (spentOutput != null)
                    {
                        storage.RemoveUnspentOutput(spentOutput.TransactionHash, spentOutput.OutputNumber);
                        storage.AddSpentOutput(spentOutput);
                    }
                    if (outputState.NewOutput != null)
                    {
                        storage.AddUnspentOutput(outputState.NewOutput);
                    }
                }
            }
            transactions.Clear();
        }

        private TransactionState LoadTransaction(byte[] transactionHash)
        {
            TransactionState transactionState;
            if (transactions.TryGetValue(transactionHash, out transactionState))
            {
                return transactionState;
            }

            transactionState = new TransactionState();
            transactions.Add(transactionHash, transactionState);

            List<UnspentOutput> outputs = storage.FindUnspentOutputs(transactionHash);
            foreach (UnspentOutput output in outputs)
            {
                OutputState outputState = new OutputState();
                transactionState.Outputs.Add(output.OutputNumber, outputState);

                outputState.StoredOutput = output;
            }

            return transactionState;
        }

        private class TransactionState
        {
            public Dictionary<int, OutputState> Outputs { get; } = new Dictionary<int, OutputState>();
        }

        private class OutputState
        {
            public UnspentOutput StoredOutput { get; set; }

            public SpentOutput SpentOutput { get; set; }

            public UnspentOutput NewOutput { get; set; }

            public UnspentOutput CurrentOutput
            {
                get
                {
                    if (StoredOutput != null && SpentOutput == null)
                    {
                        return StoredOutput;
                    }
                    return NewOutput;
                }
            }
        }
    }
}