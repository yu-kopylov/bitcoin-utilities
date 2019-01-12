using System.Collections.Generic;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Rules
{
    public interface IUpdatableOutputSet<TOutput> where TOutput : ISpendableOutput
    {
        IReadOnlyCollection<TOutput> FindUnspentOutputs(byte[] transactionHash);
        TOutput FindUnspentOutput(TxOutPoint outPoint);

        void AddUnspent(byte[] txHash, int outputIndex, int height, TxOut txOut);
        void Spend(TOutput output, int blockHeight);
    }
}