using BitcoinUtilities.Node.Modules.Outputs.Events;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.Scripts;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Modules.Outputs
{
    public class SignatureValidationService : EventHandlingService
    {
        private readonly NetworkParameters networkParameters;
        private readonly IEventDispatcher eventDispatcher;

        public SignatureValidationService(NetworkParameters networkParameters, IEventDispatcher eventDispatcher)
        {
            this.networkParameters = networkParameters;
            this.eventDispatcher = eventDispatcher;

            On<SignatureValidationRequest>(ValidateSignatures);
        }

        private void ValidateSignatures(SignatureValidationRequest request)
        {
            var enumerator = new ConcurrentEnumerator<ProcessedTransaction>(request.Transactions);
            enumerator.ProcessWith(new object[4], (_, en) => VerifySignatures(request.Header.Hash, request.Header.Timestamp, en));

            // if we are here, then no exceptions were thrown during validation and transactions are valid
            eventDispatcher.Raise(new SignatureValidationResponse(request.Header, true));
        }

        private object VerifySignatures(byte[] blockHash, uint timestamp, IConcurrentEnumerator<ProcessedTransaction> processedTransactions)
        {
            ScriptProcessor scriptProcessor = new ScriptProcessor();
            while (processedTransactions.GetNext(out var transaction))
            {
                // explicitly define coinbase transaction?
                if (transaction.Inputs.Length != 0)
                {
                    for (int inputIndex = 0; inputIndex < transaction.Inputs.Length; inputIndex++)
                    {
                        if (!VerifySignature(scriptProcessor, timestamp, transaction, inputIndex))
                        {
                            throw new BitcoinProtocolViolationException(
                                $"The transaction '{HexUtils.GetString(transaction.Transaction.Hash)}'" +
                                $" in block '{HexUtils.GetString(blockHash)}'" +
                                $" has an invalid signature script for input {inputIndex}.");
                        }
                    }
                }
            }

            return null;
        }

        private bool VerifySignature(ScriptProcessor scriptProcessor, uint timestamp, ProcessedTransaction transaction, int inputIndex)
        {
            scriptProcessor.Reset();

            ISigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(transaction.Transaction);
            // todo: create hash calculator with scriptProcessor?
            scriptProcessor.SigHashCalculator = networkParameters.SigHashCalculatorFactory.CreateCalculator(timestamp, transaction.Transaction);

            sigHashCalculator.InputIndex = inputIndex;
            sigHashCalculator.Amount = transaction.Inputs[inputIndex].Value;

            scriptProcessor.Execute(transaction.Transaction.Inputs[inputIndex].SignatureScript);
            scriptProcessor.Execute(transaction.Inputs[inputIndex].PubKeyScript);

            return scriptProcessor.Valid && scriptProcessor.Success;
        }
    }
}