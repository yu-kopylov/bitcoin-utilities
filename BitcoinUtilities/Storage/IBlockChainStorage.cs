﻿using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Storage
{
    //todo: add methods and XMLDOC
    public interface IBlockChainStorage
    {
        void AddHeaders(BlockHeader[] headers);
        BlockLocator GetCurrentChainLocator();
    }
}