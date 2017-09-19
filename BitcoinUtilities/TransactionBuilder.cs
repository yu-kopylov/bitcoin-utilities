using System;
using System.Collections.Generic;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;

namespace BitcoinUtilities
{
    public class TransactionBuilder
    {
        private readonly List<Input> inputs = new List<Input>();
        private readonly List<Output> outputs = new List<Output>();

        public void AddInput(byte[] outputTransactionHash, int outputNumber, byte[] pubkeyScript, byte[] privateKey, bool isCompressedAddress)
        {
            // todo: validate all parameters
            // todo: add xml-doc

            Input input = new Input();

            input.OutputTransactionHash = outputTransactionHash;
            input.OutputNumber = outputNumber;
            input.PubkeyScript = pubkeyScript;
            input.PrivateKey = privateKey;
            input.IsCompressedAddress = isCompressedAddress;

            inputs.Add(input);
        }

        public void AddOutput(byte[] pubkeyScript, ulong value)
        {
            // todo: validate all parameters
            // todo: add xml-doc

            Output output = new Output();

            output.PubkeyScript = pubkeyScript;
            output.Value = value;

            outputs.Add(output);
        }

        /// <summary>
        /// Generates a transaction based on the current state.
        /// </summary>
        /// <returns>A generated transaction.</returns>
        /// <exception cref="InvalidOperationException">If transaction cannot be generated from the current state.</exception>
        public Tx Build()
        {
            // todo: validate all parameters

            TxIn[] txInputs = new TxIn[inputs.Count];
            for (int i = 0; i < inputs.Count; i++)
            {
                Input input = inputs[i];
                // todo: review constant
                txInputs[i] = new TxIn(new TxOutPoint(input.OutputTransactionHash, input.OutputNumber), null, 0xFFFFFFFF);
            }

            TxOut[] txOutputs = new TxOut[outputs.Count];
            for (int i = 0; i < outputs.Count; i++)
            {
                Output output = outputs[i];
                txOutputs[i] = new TxOut(output.Value, output.PubkeyScript);
            }

            // todo: review constant
            Tx transaction = new Tx(1, txInputs, txOutputs, 0xFFFFFFFF);

            for (int i = 0; i < inputs.Count; i++)
            {
                Input input = inputs[i];

                byte[] signatureScript;

                if (BitcoinScript.IsPayToPubkeyHash(input.PubkeyScript))
                {
                    string outputAddress = BitcoinScript.GetAddressFromPubkeyScript(input.PubkeyScript);
                    byte[] publicKey = BitcoinPrivateKey.ToEncodedPublicKey(input.PrivateKey, input.IsCompressedAddress);
                    string privateKeyAddress = BitcoinAddress.FromPublicKey(publicKey);
                    if (outputAddress != privateKeyAddress)
                    {
                        throw new InvalidOperationException($"Address in PubkeyScript does not match private key address for input #{i}: '{outputAddress}', '{privateKeyAddress}'.");
                    }

                    // todo: is SIGHASH_ALL defined by output PubkeyScript ?
                    byte[] signedData = ScriptProcessor.GetSignedData(transaction, 0, input.PubkeyScript, 1 /*SIGHASH_ALL*/);
                    byte[] signature = SignatureUtils.Sign(signedData, input.PrivateKey, input.IsCompressedAddress);
                    signatureScript = BitcoinScript.CreatePayToPubkeyHashSignature(signature, publicKey, 1 /*SIGHASH_ALL*/);
                }
                else
                {
                    throw new InvalidOperationException($"PubkeyScript for input #{i} has unknown format.");
                }

                txInputs[i] = new TxIn(new TxOutPoint(input.OutputTransactionHash, input.OutputNumber), signatureScript, 0xFFFFFFFF);
            }

            return transaction;
        }

        private class Input
        {
            public byte[] OutputTransactionHash { get; set; }
            public int OutputNumber { get; set; }
            public byte[] PubkeyScript { get; set; }
            public byte[] PrivateKey { get; set; }
            public bool IsCompressedAddress { get; set; }
        }

        private class Output
        {
            public byte[] PubkeyScript { get; set; }
            public ulong Value { get; set; }
        }
    }
}