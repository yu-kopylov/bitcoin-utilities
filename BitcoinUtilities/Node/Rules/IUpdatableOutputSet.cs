﻿using System.Collections.Generic;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Node.Rules
{
    public interface IUpdatableOutputSet<TOutput> where TOutput : ISpendableOutput
    {
        IReadOnlyCollection<TOutput> FindUnspentOutputs(byte[] transactionHash);
        TOutput FindUnspentOutput(TxOutPoint outPoint);

        void CreateUnspentOutput(byte[] txHash, int outputIndex, ulong value, byte[] pubkeyScript);
        void Spend(TOutput output);
    }
}