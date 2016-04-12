﻿using System.IO;
using BitcoinUtilities.GUI.Models;
using NUnit.Framework;

namespace Test.BitcoinUtilities.GUI.Models
{
    [TestFixture]
    public class TestSettings
    {
        [Test]
        public void Test()
        {
            Settings settings = new Settings();

            Assert.That(Settings.SettingsFolder, Is.StringEnding("GUI"));
            Assert.That(Settings.SettingsFilename, Is.EqualTo("settings.json"));

            Assert.That(settings.BlockchainFolder, Is.StringEnding("Blockchain"));
            Assert.That(settings.WalletFolder, Is.StringEnding("Wallet"));

            settings.BlockchainFolder = "test-blockchain";
            settings.WalletFolder = "test-wallet";

            Assert.False(settings.Load("non-existing-folder"));

            Assert.That(settings.BlockchainFolder, Is.EqualTo("test-blockchain"));
            Assert.That(settings.WalletFolder, Is.EqualTo("test-wallet"));

            string testFolder = Path.Combine("tmp-test", "TestSettings");

            settings.Save(testFolder);

            settings.BlockchainFolder = "test-blockchain2";
            settings.WalletFolder = "test-wallet2";

            Assert.That(settings.BlockchainFolder, Is.EqualTo("test-blockchain2"));
            Assert.That(settings.WalletFolder, Is.EqualTo("test-wallet2"));

            Assert.True(settings.Load(testFolder));

            Assert.That(settings.BlockchainFolder, Is.EqualTo("test-blockchain"));
            Assert.That(settings.WalletFolder, Is.EqualTo("test-wallet"));

            File.WriteAllText(Path.Combine(testFolder, Settings.SettingsFilename), "not json");
            Assert.False(settings.Load(testFolder));

            Assert.That(settings.BlockchainFolder, Is.EqualTo("test-blockchain"));
            Assert.That(settings.WalletFolder, Is.EqualTo("test-wallet"));
        }
    }
}