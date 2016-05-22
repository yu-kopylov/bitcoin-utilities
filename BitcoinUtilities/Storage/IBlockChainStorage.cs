namespace BitcoinUtilities.Storage
{
    //todo: add methods and XMLDOC
    public interface IBlockChainStorage
    {
        BlockLocator GetCurrentChainLocator();

        StoredBlock FindBlockByHash(byte[] hash);

        StoredBlock FindBlockByHeight(int height);

        void AddBlock(StoredBlock block);
    }
}