using System;
using BitcoinUtilities.Node.Rules;
using BitcoinUtilities.Node.Services.Outputs.Events;
using BitcoinUtilities.Scripts;
using BitcoinUtilities.Threading;

namespace BitcoinUtilities.Node.Services.Outputs
{
    public class SignatureValidationService : EventHandlingService
    {
        private readonly IEventDispatcher eventDispatcher;

        public SignatureValidationService(IEventDispatcher eventDispatcher)
        {
            this.eventDispatcher = eventDispatcher;

            On<SignatureValidationRequest>(ValidateSignatures);
        }

        private void ValidateSignatures(SignatureValidationRequest request)
        {
            var enumerator = new ConcurrentEnumerator<ProcessedTransaction>(request.Transactions);
            enumerator.ProcessWith(new object[4], (_, en) => VerifySignatures(request.Header.Hash, en));

            // if we are here, then no exceptions were thrown during validation and transactions are valid
            eventDispatcher.Raise(new SignatureValidationResponse(request.Header, true));
        }

        private object VerifySignatures(byte[] blockHash, IConcurrentEnumerator<ProcessedTransaction> processedTransactions)
        {
            ScriptProcessor scriptProcessor = new ScriptProcessor();
            while (processedTransactions.GetNext(out var transaction))
            {
                // explicitly define coinbase transaction?
                if (transaction.Inputs.Length != 0)
                {
                    for (int inputIndex = 0; inputIndex < transaction.Inputs.Length; inputIndex++)
                    {
                        if (!VerifySignature(scriptProcessor, transaction, inputIndex))
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

        private bool VerifySignature(ScriptProcessor scriptProcessor, ProcessedTransaction transaction, int inputIndex)
        {
            scriptProcessor.Reset();

            ISigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(transaction.Transaction);
            scriptProcessor.SigHashCalculator = sigHashCalculator;

            sigHashCalculator.InputIndex = inputIndex;
            sigHashCalculator.Amount = transaction.Inputs[inputIndex].Value;

            scriptProcessor.Execute(transaction.Transaction.Inputs[inputIndex].SignatureScript);
            scriptProcessor.Execute(transaction.Inputs[inputIndex].PubKeyScript);

            return scriptProcessor.Valid && scriptProcessor.Success;
        }
    }
}