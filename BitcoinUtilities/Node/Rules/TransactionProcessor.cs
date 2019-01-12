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
        private readonly ScriptParser sctriptParser = new ScriptParser();

        // todo: add tests
        public void UpdateOutputs<TOutput>
        (
            IUpdatableOutputSet<TOutput> outputs,
            int blockHeight,
            byte[] blockHash,
            BlockMessage blockMessage
        ) where TOutput : ISpendableOutput
        {
            ulong inputsSum = GetBlockReward(blockHeight);
            ulong outputsSum = 0;

            for (int transactionNumber = 0; transactionNumber < blockMessage.Transactions.Length; transactionNumber++)
            {
                Tx transaction = blockMessage.Transactions[transactionNumber];

                ulong transactionInputsSum = 0;
                ulong transactionOutputsSum = 0;

                byte[] transactionHash = CryptoUtils.DoubleSha256(BitcoinStreamWriter.GetBytes(transaction.Write));

                IReadOnlyCollection<TOutput> unspentOutputs = outputs.FindUnspentOutputs(transactionHash);
                if (unspentOutputs.Any())
                {
                    //todo: use network settings
                    if (blockHeight == 91842 || blockHeight == 91880)
                    {
                        // this blocks are exceptions from BIP-30
                        foreach (TOutput unspentOutput in unspentOutputs)
                        {
                            outputs.Spend(unspentOutput, blockHeight);
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
                if (transactionNumber != 0)
                {
                    foreach (TxIn input in transaction.Inputs)
                    {
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

                        if (!sctriptParser.TryParse(input.SignatureScript, out var inputCommands))
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

                        //todo: check signature for the output
                        outputs.Spend(output, blockHeight);
                    }
                }

                for (int outputNumber = 0; outputNumber < transaction.Outputs.Length; outputNumber++)
                {
                    TxOut output = transaction.Outputs[outputNumber];
                    //todo: check for overflow
                    transactionOutputsSum += output.Value;

                    List<ScriptCommand> commands;
                    if (!sctriptParser.TryParse(output.PubkeyScript, out commands))
                    {
                        //todo: how Bitcoin Core works in this scenario?
                        throw new BitcoinProtocolViolationException(
                            $"The output of transaction '{HexUtils.GetString(transactionHash)}'" +
                            $" in block '{HexUtils.GetString(blockHash)}'" +
                            $" has an invalid pubkey script.");
                    }

                    outputs.CreateUnspentOutput(transactionHash, outputNumber, blockHeight, output);
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

            if (inputsSum != outputsSum)
            {
                throw new BitcoinProtocolViolationException(
                    $"The sum of the inputs and the reward" +
                    $" in the block '{HexUtils.GetString(blockHash)}'" +
                    $" does not match the sum of the outputs.");
            }
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