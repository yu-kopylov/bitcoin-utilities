using System.Collections.Generic;

namespace BitcoinUtilities.Storage
{
    //todo: add methods and XMLDOC
    public interface IUnspentOutputStorage
    {
        List<UnspentOutput> FindUnspentOutputs(byte[] transactionHash);

        void AddUnspentOutput(UnspentOutput unspentOutput);

        void RemoveUnspentOutput(byte[] transactionHash, int outputNumber);

        void AddSpentOutput(SpentOutput spentOutput);
    }
}