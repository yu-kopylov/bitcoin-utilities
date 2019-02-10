using System.Collections.Generic;
using System.Linq;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Messages;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Scripts;

namespace BitcoinUtilities.Node.Rules
{
    public class TransactionProcessor
    {
        private readonly ScriptParser scriptParser = new ScriptParser();

        // todo: add tests
        public ProcessedTransaction[] UpdateOutputs<TOutput>
        (
            IUpdatableOutputSet<TOutput> outputs,
            int blockHeight,
            byte[] blockHash,
            BlockMessage blockMessage
        ) where TOutput : ISpendableOutput
        {
            ulong inputsSum = GetBlockReward(blockHeight);
            ulong outputsSum = 0;

            ProcessedTransaction[] processedTransactions = new ProcessedTransaction[blockMessage.Transactions.Length];

            for (int transactionNumber = 0; transactionNumber < blockMessage.Transactions.Length; transactionNumber++)
            {
                Tx transaction = blockMessage.Transactions[transactionNumber];

                ulong transactionInputsSum = 0;
                ulong transactionOutputsSum = 0;

                byte[] transactionHash = transaction.Hash;

                IReadOnlyCollection<TOutput> unspentOutputs = outputs.FindUnspentOutputs(transactionHash);
                if (unspentOutputs.Any())
                {
                    //todo: use network settings
                    if (blockHeight == 91842 || blockHeight == 91880)
                    {
                        // those blocks are exceptions from BIP-30
                        foreach (TOutput output in unspentOutputs)
                        {
                            outputs.Spend(output);
                        }
                    }
                    else
                    {
                        throw new BitcoinProtocolViolationException(
                            $"The transaction '{HexUtils.GetString(transactionHash)}'" +
                            $" in block '{HexUtils.GetString(blockHash)}'" +
                            $" has same hash as an existing unspent transaction (see BIP-30).");
                    }
                }

                //todo: check transaction hash against genesis block transaction hash
                if (transactionNumber == 0)
                {
                    processedTransactions[transactionNumber] = new ProcessedTransaction(transaction, new TransactionInput[0]);
                }
                else
                {
                    TransactionInput[] transactionInputs = new TransactionInput[transaction.Inputs.Length];

                    for (int inputNum = 0; inputNum < transaction.Inputs.Length; inputNum++)
                    {
                        TxIn input = transaction.Inputs[inputNum];
                        TOutput output = outputs.FindUnspentOutput(input.PreviousOutput);

                        if (output == null)
                        {
                            throw new BitcoinProtocolViolationException(
                                $"The input of the transaction '{HexUtils.GetString(transactionHash)}'" +
                                $" in block '{HexUtils.GetString(blockHash)}'" +
                                $" has been already spent or did not exist.");
                        }

                        //todo: check for overflow
                        transactionInputsSum += output.Value;

                        if (!scriptParser.TryParse(input.SignatureScript, out var inputCommands))
                        {
                            throw new BitcoinProtocolViolationException(
                                $"The transaction '{HexUtils.GetString(transactionHash)}'" +
                                $" in block '{HexUtils.GetString(blockHash)}'" +
                                $" has an invalid signature script.");
                        }

                        if (inputCommands.Any(c => !IsValidSignatureCommand(c.Code)))
                        {
                            throw new BitcoinProtocolViolationException(
                                $"The transaction '{HexUtils.GetString(transactionHash)}'" +
                                $" in block '{HexUtils.GetString(blockHash)}'" +
                                $" has forbidden commands in the signature script.");
                        }

                        outputs.Spend(output);
                        transactionInputs[inputNum] = new TransactionInput(output.Value, output.PubkeyScript);
                    }

                    processedTransactions[transactionNumber] = new ProcessedTransaction(transaction, transactionInputs);
                }

                for (int outputNumber = 0; outputNumber < transaction.Outputs.Length; outputNumber++)
                {
                    TxOut output = transaction.Outputs[outputNumber];
                    //todo: check for overflow
                    transactionOutputsSum += output.Value;

                    List<ScriptCommand> commands;
                    if (!scriptParser.TryParse(output.PubkeyScript, out commands))
                    {
                        //todo: how Bitcoin Core works in this scenario?
                        throw new BitcoinProtocolViolationException(
                            $"The output of transaction '{HexUtils.GetString(transactionHash)}'" +
                            $" in block '{HexUtils.GetString(blockHash)}'" +
                            $" has an invalid pubkey script.");
                    }

                    outputs.CreateUnspentOutput(transactionHash, outputNumber, output.Value, output.PubkeyScript);
                }

                if (transactionNumber != 0 && transactionInputsSum < transactionOutputsSum)
                {
                    // for coinbase transaction output sum is checked later as part of total block inputs, outputs and reward sums equation
                    throw new BitcoinProtocolViolationException(
                        $"The sum of the inputs in the transaction '{HexUtils.GetString(transactionHash)}'" +
                        $" in block '{HexUtils.GetString(blockHash)}'" +
                        $" is less than the sum of the outputs.");
                }

                //todo: check for overflow
                inputsSum += transactionInputsSum;
                //todo: check for overflow
                outputsSum += transactionOutputsSum;
            }

            if (inputsSum < outputsSum)
            {
                // in block 124724 inputsSum is less than outputsSum
                throw new BitcoinProtocolViolationException(
                    $"The sum of the inputs and the reward" +
                    $" in the block '{HexUtils.GetString(blockHash)}'" +
                    $" is less than the sum of the outputs ({inputsSum} < {outputsSum}).");
            }

            return processedTransactions;
        }

        private static bool IsValidSignatureCommand(byte code)
        {
            // note: OP_RESERVED (0x50) is considered to be a push-only command
            return code <= BitcoinScript.OP_16;
        }

        private static ulong GetBlockReward(int height)
        {
            //todo: use network settings
            ulong reward = 5000000000;
            //todo: use network settings
            while (height >= 210000)
            {
                height -= 210000;
                reward /= 2;
            }

            return reward;
        }
    }
}