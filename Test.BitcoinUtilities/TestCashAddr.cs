using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestCashAddr
    {
        [Test]
        public void TestEncode()
        {
            byte[] publicKeyHash;
            BitcoinNetworkKind networkKind;
            BitcoinAddressUsage addressUsage;

            // examples from: https://github.com/bitcoincashorg/bitcoincash.org/blob/master/spec/cashaddr.md

            Assert.True(BitcoinAddress.TryDecode("1BpEi6DfDAUFd7GtittLSdBeYJvcoaVggu", out networkKind, out addressUsage, out publicKeyHash));
            Assert.That(CashAddr.Encode("bitcoincash", addressUsage, publicKeyHash), Is.EqualTo("bitcoincash:qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a"));

            Assert.True(BitcoinAddress.TryDecode("1KXrWXciRDZUpQwQmuM1DbwsKDLYAYsVLR", out networkKind, out addressUsage, out publicKeyHash));
            Assert.That(CashAddr.Encode("bitcoincash", addressUsage, publicKeyHash), Is.EqualTo("bitcoincash:qr95sy3j9xwd2ap32xkykttr4cvcu7as4y0qverfuy"));

            Assert.True(BitcoinAddress.TryDecode("16w1D5WRVKJuZUsSRzdLp9w3YGcgoxDXb", out networkKind, out addressUsage, out publicKeyHash));
            Assert.That(CashAddr.Encode("bitcoincash", addressUsage, publicKeyHash), Is.EqualTo("bitcoincash:qqq3728yw0y47sqn6l2na30mcw6zm78dzqre909m2r"));

            Assert.True(BitcoinAddress.TryDecode("3CWFddi6m4ndiGyKqzYvsFYagqDLPVMTzC", out networkKind, out addressUsage, out publicKeyHash));
            Assert.That(CashAddr.Encode("bitcoincash", addressUsage, publicKeyHash), Is.EqualTo("bitcoincash:ppm2qsznhks23z7629mms6s4cwef74vcwvn0h829pq"));

            Assert.True(BitcoinAddress.TryDecode("3LDsS579y7sruadqu11beEJoTjdFiFCdX4", out networkKind, out addressUsage, out publicKeyHash));
            Assert.That(CashAddr.Encode("bitcoincash", addressUsage, publicKeyHash), Is.EqualTo("bitcoincash:pr95sy3j9xwd2ap32xkykttr4cvcu7as4yc93ky28e"));

            Assert.True(BitcoinAddress.TryDecode("31nwvkZwyPdgzjBJZXfDmSWsC4ZLKpYyUw", out networkKind, out addressUsage, out publicKeyHash));
            Assert.That(CashAddr.Encode("bitcoincash", addressUsage, publicKeyHash), Is.EqualTo("bitcoincash:pqq3728yw0y47sqn6l2na30mcw6zm78dzq5ucqzc37"));
        }
    }
}