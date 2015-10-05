﻿using System;
using BitcoinUtilities;
using NUnit.Framework;

namespace Test.BitcoinUtilities
{
    [TestFixture]
    public class TestBitcoinAddress
    {
        [Test]
        public void TestFromPrivateKeyUncompressed()
        {
            // Reference example
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0x18, 0xE1, 0x4A, 0x7B, 0x6A, 0x30, 0x7F, 0x42, 0x6A, 0x94, 0xF8, 0x11, 0x47, 0x01, 0xE7, 0xC8,
                                                          0xE7, 0x74, 0xE7, 0xF9, 0xA4, 0x7E, 0x2C, 0x20, 0x35, 0xDB, 0x29, 0xA2, 0x06, 0x32, 0x17, 0x25
                                                      }, false), Is.EqualTo("16UwLL9Risc3QfPqBUvKofHmBQ7wMtjvM"));

            // BIP-38 example
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0x64, 0xEE, 0xAB, 0x5F, 0x9B, 0xE2, 0xA0, 0x1A, 0x83, 0x65, 0xA5, 0x79, 0x51, 0x1E, 0xB3, 0x37,
                                                          0x3C, 0x87, 0xC4, 0x0D, 0xA6, 0xD2, 0xA2, 0x5F, 0x05, 0xBD, 0xA6, 0x8F, 0xE0, 0x77, 0xB6, 0x6E
                                                      }, false), Is.EqualTo("16ktGzmfrurhbhi6JGqsMWf7TyqK9HNAeF"));

            // X starts with 0 
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x99
                                                      }, false), Is.EqualTo("1FVgHxdCE4sjHoNTVmoygXRWcKzdyXvGhm"));

            // Y starts with 0 
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                          0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7A
                                                      }, false), Is.EqualTo("1NwhxtuUW2dg7RPkaypDpLW9JS2dqfEzNb"));
        }

        [Test]
        public void TestFromPrivateKeyCompressed()
        {
            // BIP-38 example
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0xCB, 0xF4, 0xB9, 0xF7, 0x04, 0x70, 0x85, 0x6B, 0xB4, 0xF4, 0x0F, 0x80, 0xB8, 0x7E, 0xDB, 0x90,
                                                          0x86, 0x59, 0x97, 0xFF, 0xEE, 0x6D, 0xF3, 0x15, 0xAB, 0x16, 0x6D, 0x71, 0x3A, 0xF4, 0x33, 0xA5
                                                      }, true), Is.EqualTo("164MQi977u9GUteHr4EPH27VkkdxmfCvGW"));
            // BIP-38 example
            Assert.That(BitcoinAddress.FromPrivateKey(new byte[]
                                                      {
                                                          0x09, 0xC2, 0x68, 0x68, 0x80, 0x09, 0x5B, 0x1A, 0x4C, 0x24, 0x9E, 0xE3, 0xAC, 0x4E, 0xEA, 0x8A,
                                                          0x01, 0x4F, 0x11, 0xE6, 0xF9, 0x86, 0xD0, 0xB5, 0x02, 0x5A, 0xC1, 0xF3, 0x9A, 0xFB, 0xD9, 0xAE
                                                      }, true), Is.EqualTo("1HmPbwsvG5qJ3KJfxzsZRZWhbm1xBMuS8B"));
        }

        [Test]
        public void TestValidation()
        {
            Assert.Throws<ArgumentException>(() => BitcoinAddress.FromPrivateKey(null, true));
            Assert.Throws<ArgumentException>(() => BitcoinAddress.FromPrivateKey(new byte[32], true));
        }
    }
}