using System.Diagnostics.CodeAnalysis;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public class TestCashAddr
    {
        [Test]
        public void TestEncodeDecode()
        {
            // examples from: https://github.com/bitcoincashorg/bitcoincash.org/blob/master/spec/cashaddr.md
            TestEncodeDecode("1BpEi6DfDAUFd7GtittLSdBeYJvcoaVggu", "bitcoincash:qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a");
            TestEncodeDecode("1KXrWXciRDZUpQwQmuM1DbwsKDLYAYsVLR", "bitcoincash:qr95sy3j9xwd2ap32xkykttr4cvcu7as4y0qverfuy");
            TestEncodeDecode("16w1D5WRVKJuZUsSRzdLp9w3YGcgoxDXb", "bitcoincash:qqq3728yw0y47sqn6l2na30mcw6zm78dzqre909m2r");
            TestEncodeDecode("3CWFddi6m4ndiGyKqzYvsFYagqDLPVMTzC", "bitcoincash:ppm2qsznhks23z7629mms6s4cwef74vcwvn0h829pq");
            TestEncodeDecode("3LDsS579y7sruadqu11beEJoTjdFiFCdX4", "bitcoincash:pr95sy3j9xwd2ap32xkykttr4cvcu7as4yc93ky28e");
            TestEncodeDecode("31nwvkZwyPdgzjBJZXfDmSWsC4ZLKpYyUw", "bitcoincash:pqq3728yw0y47sqn6l2na30mcw6zm78dzq5ucqzc37");
        }

        private void TestEncodeDecode(string bitcoinAddress, string cashAddress)
        {
            Assert.True(BitcoinAddress.TryDecode(bitcoinAddress, out _, out var addressUsage, out var publicKeyHash));
            Assert.That(CashAddr.Encode("bitcoincash", addressUsage, publicKeyHash), Is.EqualTo(cashAddress));

            Assert.True(CashAddr.TryDecode(cashAddress, out string decodedNetworkPrefix, out var decodedAddressUsage, out var decodedPublicKeyHash));

            Assert.That(decodedNetworkPrefix, Is.EqualTo("bitcoincash"));
            Assert.That(decodedAddressUsage, Is.EqualTo(addressUsage));
            Assert.That(decodedPublicKeyHash, Is.EqualTo(publicKeyHash));
        }
    }
}