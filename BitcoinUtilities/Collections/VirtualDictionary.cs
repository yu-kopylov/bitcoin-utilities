using System;
using BitcoinUtilities.Collections.VirtualDictionaryInternals;

namespace BitcoinUtilities.Collections
{
    public class VirtualDictionary : IDisposable
    {
        private readonly VirtualDictionaryContainer container;

        private VirtualDictionary(string filename, int keySize, int valueSize)
        {
            container = new VirtualDictionaryContainer(filename, keySize, valueSize);
        }

        public static VirtualDictionary Open(string filename, int keySize, int valueSize)
        {
            return new VirtualDictionary(filename, keySize, valueSize);
        }

        public void Dispose()
        {
            //todo: implement
            container.Dispose();
        }

        internal VirtualDictionaryContainer Container
        {
            get { return container; }
        }

        public VirtualDictionaryTransaction BeginTransaction()
        {
            return new VirtualDictionaryTransaction(this);
        }
    }
}