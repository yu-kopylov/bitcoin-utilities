using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using BitcoinUtilities.Scripts;
using NUnit.Framework;

namespace Test.BitcoinUtilities.Scripts
{
    [TestFixture]
    public class TestBitcoinCoreSigHashCalculator
    {
        [Test]
        public void TestExamples([ValueSource(nameof(GetExamples))] TransactionInputExample example)
        {
            ISigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(example.Transaction);
            sigHashCalculator.InputIndex = example.InputIndex;
            sigHashCalculator.Amount = example.InputValue;

            ScriptProcessor scriptProcessor = new ScriptProcessor();
            scriptProcessor.SigHashCalculator = sigHashCalculator;

            scriptProcessor.Execute(example.Transaction.Inputs[example.InputIndex].SignatureScript);
            scriptProcessor.Execute(example.InputScript);

            Assert.That(scriptProcessor.Valid);
            Assert.True(scriptProcessor.Success);
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static IEnumerable<TransactionInputExample> GetExamples()
        {
            yield return new TransactionInputExample
            (
                name: "All",
                blockHeight: 233711,
                transaction: "0100000003581d5914f4ff45106cb2095b6d8fd69c1948d7740b737240d8f9643af5eef54a010000006b483045022100bb0057785e90766ecb07951c1f609b9ac" +
                             "035555761dd27a57b817407cd3728b30220078feaf1c6fb84577c6cb43cd05f0cef1511aa78b282095c32b28bf85fd89d7c012102b8216c9a88eac11464c164d8" +
                             "d3d7fc354740d1a90855a79ce107a3e11c98558affffffff4717c0f1630f490cdbdda5d9be02e425b8490b83ab04ba0e77e25b194575347a000000006b4830450" +
                             "22100f219fb37109196da43f4181c2de30e95eff279f53139c1287adac1cfa32e114b02206ee5c3d64028e52986c4dd9609a33a67776b9d530e36c5d66496b019" +
                             "3ae18cb9012103130adaf9d837b152fedda51e3abe4071a8c121bc2c28d28bb3ae9a60b362b7a8ffffffff693bdb5392c9ef18310a58ca9a81faf6b3723167354" +
                             "e2f3ddd1b8cbff1ac075a000000006c493046022100d6fbab3c22e93169c2b00af5ddffc5dd97cffc8cb980751dff3ce115411b2bfa022100e19a2496686b807c" +
                             "4dafe25db51d047c202e562995d778af6a492cc4d44ab0c9012102e40c8d820733fdc959b75c48d5e23e6d6f3abf56b7e4ad53b9b6126313367543ffffffff025" +
                             "9d50f00000000001976a914daddf0d17066a475f1aa813759f0fc990f70d89a88acc447f00a000000001976a914f3d17d6d58c30445358b28b47bd4a660b13053" +
                             "6c88ac00000000",
                inputIndex: 1,
                inputValue: 166987475,
                inputScript: "76a9145ffc5318392c877c2844e8aebf7860eb49bc667188ac"
            );

            yield return new TransactionInputExample
            (
                name: "All",
                blockHeight: 358475,
                transaction: "01000000025ab235029a806c6cf7ecdc9e8cb7e2908f6c7486850ec8028b239cf6c8a34451180000006a47304402205df665776efd7b6706fd57d6908a6e2d21d" +
                             "376b955d12f92dd9131321a0a8e860220495b0cf0bf6c0cd04072d52b0cd8aa677d066dabcc44e94bbd9e0a09217f7ad20121021bb1226f977e44414142338050" +
                             "80a87155237671ea4411e0d59761e3b279beb0ffffffff1197039c107ac361a8715cbe01bdaef7d9d71224dbeade58482f1ff0f0c80830160000006a473044022" +
                             "04ab083bbccac90fbe4578a7a2aada840960d0944f30b2f00a700c0691f8cc3d302200c81ce0b2055fca5a63b9b6140d6c90e4b459b691184d6382346cc458084" +
                             "2a940121021bb1226f977e4441414233805080a87155237671ea4411e0d59761e3b279beb0ffffffff01a6450000000000001976a914d7c2eb86ab5296da2dd29" +
                             "f58ba0c22de3ee66d4e88ac00000000",
                inputIndex: 1,
                inputValue: 7200,
                inputScript: "76a914b39282e92e7fa1088c3e9a247c016eaa54c65e7388ac"
            );

            yield return new TransactionInputExample
            (
                name: "All",
                blockHeight: 360260,
                transaction: "01000000023a57613b6917a4ec150afb6268c5ed8ec1b3a617d93b446f4c6d25bbe2cba164870700006a47304402207cd715e878fc0710e2d353c503719f1cdfc" +
                             "e9e33e420e856e92960b9efaf83c9022039b26d6f8fb2edf54569d17a39be646886071f3a5c07de443ac6588d72a9b27f012103e94a367d2dd27c63e3fd79e8ae" +
                             "492a533dcf3bca85bf11c5fad071f81a0587e3ffffffff5047110f0087a9a96892ab2305b67d17c53c704fc18f8238f79bfeb494fabeac080000006a473044022" +
                             "02c20ecf348936cdaed77af7373c2e3970df18aa70062bd3d8fb777aaac986cc2022041b89e8870ae3689ca5bc9e877af0e5440c48484e6a85589944ea10bca8d" +
                             "617d01210268495d98a112435c0d6603965ba9a9a7583ff394a9dcee98d0b80f041759e915ffffffff0130750000000000001976a9142c9b6211549731c6cd5a8" +
                             "b345188e2e24ae353bc88ac00000000",
                inputIndex: 1,
                inputValue: 1000,
                inputScript: "76a91461d47bd64f3b18f4c8d2dbe27a5ff3bc95a2167f88ac"
            );

            yield return new TransactionInputExample
            (
                name: "All, AnyoneCanPay",
                blockHeight: 300608,
                transaction: "01000000039fa40e205a5cecad73122c9352a1444065ef4fa42b954349ee1bd51f74c71da7020000008b48304502201a8747991cad3719123cc53b895d68af66" +
                             "6e9f92a7425395fb898858a9ee9e96022100eeaac1102466e5662bf9096207c82bd94fbae2851787cbe7de1c33bd88a927e0814104d64438ef7a2495178af6a3" +
                             "6dbe28745c24592de45d836b39c60f97dfa0a65cf416b2356e516aa94b75afd5f47aee1615d0f50a26aaf8d4d8ff8c10d0d8bc6948ffffffff9fa40e205a5cec" +
                             "ad73122c9352a1444065ef4fa42b954349ee1bd51f74c71da7010000008a47304402206007b13be1802c30901d129ef6cdfc569d897e611c18b9494f3a09f0a7" +
                             "a4c12f02205371050611b4c7ffd31611affe56062c4397f9e965416d3a29b36b0de0bcd40c814104228fcd8eeb4a76e6274b7fcb7de9972e71fb7b2c90fe9420" +
                             "29b6cf63252e893372653e424daccdf72fbd0a69dda0f6f5dfeaec2eb81212d369ef4661b97cb58bffffffff9fa40e205a5cecad73122c9352a1444065ef4fa4" +
                             "2b954349ee1bd51f74c71da7000000008c493046022100c1fe963bb931fd073206ca0791246ad5e0caae25daa8876645daf540cf958f15022100956f679a6704" +
                             "0a44a1dc6f6ecc8ab9d0e23b23734ce5ffd31bdcb783bbd66c8a814104245ed9389a3e886be0ac547f83bb9ac6f92657af877eea955aefdf2e2bea09e7d19da3" +
                             "67e3feaace826f3e42f0be045d66bb35394b9fcc66255a1ce3877ad7f0ffffffff03809698000000000017a914963e46b1eea894fe855d543d030f82fd4661e6" +
                             "0587d8dde400000000001976a91461222f04b1ff4f44deb991c837d0aaa75890fa3788acd8dde4000000000017a9145638b284408458f728e0acc56590ff6408" +
                             "b0199f8700000000",
                inputIndex: 0,
                inputValue: 20000000,
                inputScript: "76a91409530cd80b6b25068154319de7aa3ef19314d3d188ac"
            );

            yield return new TransactionInputExample
            (
                name: "All, AnyoneCanPay",
                blockHeight: 358109,
                transaction: "0100000005c7ee6e96bacfd23624f9d2eeff0746acbb3cda2124c7bcbf88a2e745b9216b90000000006a4730440220387d9cf62ce822927a312b0bec05a49b343" +
                             "07347f104d5176602cf4fccad894b022064a5baf6bda64abcd93e920e5a1adb66f7fe273a2537078a46ef5f97403b965181210239882c5325e054211848bca358" +
                             "ea872d4c80527062c0c64850c08896a7649a35ffffffff4520ea216ff66bdd17581e7eced6d996f800976e87c066879d41ead33d6bdc59000000006a473044022" +
                             "00db84a11d83cfcb99f96d05d247ada8999695d6ce9ea6a6971326dc87231bc8902202129f6111b6e97af44a7038b67501d3bc82fc08199df07e9cdfecb866b99" +
                             "afc1812102a9ef64055f1c131694b44748998fca79ed7a859a05e0879ed4cb3d0845de6cbaffffffff6d1ef3c48765c4afb64f462e253eb0d05f29f7718cd2ac4" +
                             "88e47eadb9ad52eaf000000006b483045022100fb952eaa114aaf3e24f40f5181b49eed95c95f9248f674fca835bcbb73bcfc3a022071ad1951bcdc981cf9a9f5" +
                             "96da8c0daaaa075b3d2daacdecfb9ae3fc984a034b812102735f5c74ccd625c6d561e9008588a9685127286f3d09e66da8bc514bef3d2807ffffffffaeb275b67" +
                             "b00c4ba1e2e5ea71c5c551c96eba248106f5666434bbabd0eabd58f000000006b4830450221008017b117ee3244a8fa43c8f84eead3b4546b16dce24015a015b3" +
                             "842a451336000220448f4272c9b4dc2b0542ae286041175211f4f1e3f82c9f5cdc4d59595f86433b8121023b1e114f3701cd333f4ddf8b89b6a37c52173726193" +
                             "004c086d098a6d5a3fa6fffffffff74fbf725ba60d53514aad629536c4631f5a29d997f035b566f7f45188bbf6d34000000008b483045022100cdec9d8700f3f1" +
                             "cdfaa4932166c899e63b77d49606cec01e7b12f2d3c940270c0220058277405768e5a8416b10a999a82281af4fec406a4e989b320c6a2ccf9b3c7e01410445d77" +
                             "b264464af3f1e3722234720806a34d49e5a69e162a743585fe56c3a0503248082eac522174efbe3029c35c1eba197ab79a53f7c40a4edce38ddbc2129afffffff" +
                             "ff0140597307000000001976a91422f12549ce37381e34cdffd13e45c45ca5146f8a88ac00000000",
                inputIndex: 1,
                inputValue: 4198857,
                inputScript: "76a9141d4726c4d059bbd19a6eb998cc9babebb760962b88ac"
            );

            yield return new TransactionInputExample
            (
                name: "All, AnyoneCanPay",
                blockHeight: 358288,
                transaction: "01000000156f6ebe3ba9d137dd00bcd6f43568cfff30cd7321a973f45ebf8a9edb1abc63e8000000006b483045022100b38884b1ed6d85853cd9552b64494d418" +
                             "ea59fc5b81ecf736a831448eac23b070220075ef0dc2e3c899e1ce34a09930ff6650f6facc9a6499318bf31ae0fa606976e812102fb0c3c38568efa85ebe44cfc" +
                             "dc7267314a5db79b89aa1b0a3af6cbec500154d3ffffffff0ef8570732d30bc9933ecbcc2be43271e64c9d70cb0be5d4625618efdcebd09c000000006a4730440" +
                             "2201dc146789d4baef02dab9931b86241d892d4cb057ee88f5a0ce096e7b2801c840220149f838b3543988dd5752fe59bf0ebc63cbbd767d0c8e58cb758a90be4" +
                             "7bb678812103177dbb1d05ede03d232caa55c36344e7dc6745950ef010c6e0c5c36a425789ebffffffff2c60d2191f10d2b3713b5d56f93fe13faf4da444db1a3" +
                             "a8e95a1e31b68aa7cb9000000006a47304402207602392543ef3c03a8fd88be3d6baeb1bacd3484a1dc849e55c6d4a649e3e17c0220177c0a3fdfb3aa8e512085" +
                             "4d288ddca3396362ae6630f5a2a0218a7ab187859c8121031a8ede20d013aa493ab135f398c66741f01ff632356f12447967fe957aa99d5cffffffff96bbf3c76" +
                             "d0037dc5f0a6f348c3e3ab02d941ab9474d884bc8a5e786ad578aad000000006a473044022042e206805bf75297ceaa3b368ca24b7157b42ea1867e8e24292418" +
                             "b284184ffc0220262beffddcee9eb0ef9d2ef0b656c1ef8beac55526502343b372e74177f181968121032a8e24393d093ebd3b22f588200975bb3009b33cb745c" +
                             "2e37be338759ffea51dffffffff83b9fc2d6d0025f26f436757c7a4f0267ce35d5f4f6b2bb12721b6fd41ca82f0000000006b483045022100e2c761e1dcb0ea4c" +
                             "f3acbadd35601eb6c772a6fc94d57e6886dffd659d9fbde402202a5fdca82cf0f19721270228b181935532cf6e10b863181051613f9338801a6b812102d7d2e80" +
                             "3c528996998d48f4d01582d5d106856f1ce48a0227d6f7d2f5e0e2274ffffffffacd8fa311c0ca2e5cfef414f0f1a745a9f9b24e717be1d03f2c9efe982eeed37" +
                             "010000006a47304402204906163f425c3ae4631a7773b57d932867cf3b1ef2d7fdd670207cc620db02a9022016e359bed2c5573a3907658ed9f1077eea4490b8c" +
                             "b0c4284f2e620fb4bf8b064812102c4687a1ac7426fe938ae4befcaeefead4c56825f9317bc2a3dedffce918a3c76ffffffff3396d8b591367e5d2fefe32632a9" +
                             "626a998cdb35ea26cfdd9c533907389616eb010000006a47304402202069a536e205d78d6a81572da2cdd0d1fa0f190df798f6c5ef1a95b2d88ef88002207f50f" +
                             "9bfb5e1242f365dfc9a9ebb03c17755211f98c2f5d3ccfb219956aba91c81210285c101fb49ac0e4a3bbd06b541791cf2db1870032b126953b56814c784e32711" +
                             "ffffffff413ce12505ed3582d9e3e11f173d12094d49b34834f46fc80d77d18d607d928e000000006a4730440220706d510b6d1b575a2f47629f8f0f3bf25b221" +
                             "4fab22480b123701f33238cf06e02201f6dccbc1497b6b8ae8862597c70c621966247d0694d2f810d919c51ca8bf223812103a1faaeb12f8644898e3dc38e3eed" +
                             "c09edafaf18a6d8d4b4ec685df060456db39ffffffff4184754833d3dff28d39550ac721421ed9d35d506d3d2bc3ed34ba16a686e651000000006b48304502210" +
                             "0d3861b53234c29c5cc4122c05dc13fa1a78a22ef5c7a88c3198f805360c5706e022064c0dc60a934d2e26fd7307e5163429cce0a1f9ca453f027406f396cb2d7" +
                             "d42981210207620f2913bec5f254b66d562377296eb4b09faf54f077272c6e1a6bb4c0c1deffffffffd04a2f279d3be243b30215074318df608db76bb67b9a118" +
                             "de63f75c3e62a2e0d000000006b483045022100990d67f675af1ad8fe9dca9c1c8ccd35bcafa48f20c9f11eaf78b2b6926d8e5a02201974ec4f0255cf82766ac3" +
                             "597573e2ce87cd529dae5ebfe99a9b14fe56e71a2c8121038b56a9cddd413647c1639073ffe64a78c6b72bada3dc12b4ce2001d4a3e0146bfffffffff059970ea" +
                             "a876bc6cb4e036cdcc53755f8a78025ab36159adf1fc641cef82769010000006a473044022005b94ffc537ec598a4659f1336b3aede43ee5549b67d9c3ab556fb" +
                             "aa4ac0499b022073a75cc7d9918f5e3436f7f89214a670d98a36891c08ec3ab7dd276513691d6681210394bd3dde88ef999286d5b8078f04557ed8c5722fae083" +
                             "df968e0f7420c134cfeffffffffb573c727618ce1275167d4243ecda906d62b8d96b2497d54fd727f277ed0d0dd010000006b483045022100d2e6ec1289fa3767" +
                             "9a83c914104d59b8ec17136f509ba44a6bc79dbbc467f8fa022046ad8762b8fbd5741b4c8dfad3484693dc9138cd8902cc1b28531cb8752004a18121039787206" +
                             "7776f123d4be24cf954063395dc1d2d8c2bfabda1b7b786ced31c7690ffffffff78d93a1d9a55917475912f5752e1c0f8b27a06a360b0590afe7be8754c71fca6" +
                             "010000006a47304402206a5fd6a0c7ecae77aed303153fb162cdcd420bc57c46f0419c95191766b732c002200f2b48828257677fc8ea96669103ab3f1552dadbf" +
                             "f3dbaa841081a30ee10462d812102099109679172067cec45ad1f90137634e3a8373693209eca9bad968c578cce3effffffffec9382eeab54c0397393e5042e06" +
                             "61da8e66204944371d186ef4f0521f72eea9010000006b483045022100a9e20dc2cf0de6105d3ff62c1a4298b547e8e02de1b322904b0fd4bb6e4c9a5302202f7" +
                             "35c73ed08b64343b9b41276f9009a8d7410973a5dd33dbc6eb1e26425d75c8121027120db99ba69fd1a336563ee6504a230ef1f663c99d0af55fdfb7c8952526c" +
                             "f4ffffffff3cd1fcfe57ecb5ee6ec16ad22a05471c03ba30c2d5042d19472ec1fc869347dc010000006a47304402206bfe639e02162b9fcbb442fcbdd3b77047f" +
                             "e4b9a8d1ae409e75e75c7d4bdc30f022035a91aa9e9af01b66efa72af648d83b2fb9583e7aca437a6380e40bbca1ed24c812102fed789c8f54f8a0ec9aa272c36" +
                             "5e5b8fa6fccc63433f082c758b255c09804d1effffffffc7cbfd289224d6ef20c6ca10ad21f65f2459cec05512886e856f73831840f28f000000006b483045022" +
                             "100c670880c35b121878760ed8570fd99eb57c7eed31151b340e753c48ccfba860a02200aad9dac2da20556016f35bc63324cbb733c2fdfac1c5b9f575e0cb69a" +
                             "a0d5b181210366f53ac75752b5abffa160ff57e90a5854807f749e29107cb5b0c4b902028378ffffffff4c21cdfaa45ccdc84473d51adb6b18526e59388288fee" +
                             "209f91d2efc56e5cf06000000006a473044022046744ce166775eea21561c3072d40ea7e70dc0939c74a1e752528e254a328321022014a69e59159698023230a0" +
                             "877be4ab757023168f357afabe439ce6e2db372b9281210398a3d5998fb40862f326b6e64a965b94c4fbea7635471401b8b5126a95523572ffffffffe81f1e5c7" +
                             "f652fd684e56d9f1f51fed056f093094de5703b4944e52230b12285010000006a47304402203b10714ea2b27eb8bda780ed37ead3257e24664cbf947235822d86" +
                             "aafc87b93a02203b1ba7282337ef18fe9ba74e37b59f90b9dc8e15ae0581682db6a595c7e3c963812103a402f18a9610e63faa4a3f0cc77810d4f52ff8f76095c" +
                             "661d2ef58767b76b555ffffffff007797fe2ca1df0b8a27bb46870b85d961789f63b08bdfbbab9daf328e068a9b010000006b48304502210097c39f9a37b19bac" +
                             "9adb8367fe7bf8344c76c6970d92c2b6a43b122aff1003140220655f0babc98c940a9613376f82dc01e647489109619a8a0227877be8d66cda65812103fae06be" +
                             "4d49f12ffc8aab7b60c602873941744ee41044d5e901b6edfafb06436ffffffff037fa1c41e10378dcf3143799e68d1ac098e8615dec0485057d422f59d681282" +
                             "010000006a4730440220560bebf87bf666b2d8da156160ed725e46910873039d3c24fe5792d6267fa55302204247ab90339f0af5d6f5e3272db0fff448cd53cec" +
                             "097e9d14e844d5272671402812102892d18fb166fa8505e9d737b0515f594df914719e8bd153da1ab9b09c889d511ffffffffd28637bb0c80877ef1b4a1471e2f" +
                             "ceb3b0a8d92c578b2c7bbe615c453a98370c010000006b483045022100dcb40cd39f6b1a876fa8331a46ee481710de268ea9e1d66ccf15a610f6f8ccc50220273" +
                             "3da8481e7ede94bcfd6c39d308eb633825bae141c4725d11fa19b357696ff01210233a5c1d96c2d9223e8a83a757c7803d03c33389222b1a726b5992f7e887b4e" +
                             "48ffffffff0100c2eb0b0000000017a9140136d001619faba572df2ef3d193a57ad29122d98700000000",
                inputIndex: 1,
                inputValue: 100000000,
                inputScript: "76a9148dbbd5bc14447fe5d6247b81e18064f46b144a3588ac"
            );

            yield return new TransactionInputExample
            (
                name: "All, AnyoneCanPay",
                blockHeight: 358475,
                transaction: "010000000307b19387c26c36e4bcfb39df6e7634cd3132ae88762dc4f3f4b7944bf238bdc7000000006a47304402206903722a6a14df1cdaa6c096035f9d98252" +
                             "36ce45afaa461687863da3ffc59cc02205a1eef1e48c9ca2ac0193d5e0ca69e17fdc96cc1eb8bbc092a6529a636932ffd8121038f3abaf49b8c29b8d296c81ca2" +
                             "70eaf0f9cfa5e3c362972a884e6eeffe630bc7fffffffffc20807cf99d671debb9ea3aa9091e69e6d61493164d568c2ea9b1f3b56c6de7010000006b483045022" +
                             "100a42713a0642fc0d24bc3ab0f3c16435e0bef3f0400c8556211fe2df9e24247fb022053abb7c092adfef022ce24e27b404ae5a8a32eb2e9700495edcbc8070a" +
                             "f45c0f8121038f3abaf49b8c29b8d296c81ca270eaf0f9cfa5e3c362972a884e6eeffe630bc7ffffffff7aedbbd792eb735aeff28b53046d6788b74839ded6c15" +
                             "f66e7184b628e2894b0000000006a47304402201bb6467235010926dd9eea46bbf580fc60650662fe303e10f9c77210a4a21ae30220301893d52399f10272e443" +
                             "d9aa40cc8fabb20379970cab8b567f5f31549cc97d8121038f3abaf49b8c29b8d296c81ca270eaf0f9cfa5e3c362972a884e6eeffe630bc7ffffffff013075000" +
                             "0000000001976a914d696853a7851f07f3d5b7c81f2735d2b7e79e61588ac00000000",
                inputIndex: 1,
                inputValue: 10000,
                inputScript: "76a9144d41de74b548886cbc585cb2943754c0d393e07788ac"
            );

            yield return new TransactionInputExample
            (
                name: "None",
                blockHeight: 357233,
                transaction: "010000000195231c93650bd9009997fa4d167c3719935c182562e24ef3690e1d78893adba7010000006b483045022100ac810e7f01557b6142d0d324abd88c37" +
                             "d1f5bd097214bfeb2b878decc42088d402203a250f250ec5503a815adc2d119fbc6efa373ac79e1110364161478cbeb49a780221033978e80e30fe76867b11d1" +
                             "af3331ab0cd70e7a7242867e19b44ae3c64bda2adeffffffff0170170000000000001976a9144754ac901104098c23184986799ddd814dede65388ac00000000",
                inputIndex: 0,
                inputValue: 11332,
                inputScript: "76a9144754ac901104098c23184986799ddd814dede65388ac"
            );

            yield return new TransactionInputExample
            (
                name: "None",
                blockHeight: 357235,
                transaction: "0100000003ff067746e23b0994d86b06ff28799098c8631ddb0f9c9f84ec79fe03c5e5ea4c000000006b483045022100b518e5d0a1d8b30279bd4337c5be6c66" +
                             "5e9c42cb154cac9812e18b5abd3d4e6402205b86070023a339abfd2b0158143a42852ec6567ec87c32950eccbac282082e0f0221033978e80e30fe76867b11d1" +
                             "af3331ab0cd70e7a7242867e19b44ae3c64bda2adeffffffff4ef45c1d5a3243615baa50d53acafb5168544ad9175a32a3231114210d4e6161000000006b4830" +
                             "45022100e79bc6b1dd0bc01082a73b760795ce74c78398b11edbe97b2f6e5199afcc24e202200913b084841eed9551fc814e3453c99380d908e01e5d9a36b329" +
                             "d3adbfeb97280221020d13a5bb2a673f5e7e2bb21d1f50c58f34b8eeb59d6e782b802bbb8af4d234d5ffffffff0bb80b9719414ab6f674b756cdb1a573000d41" +
                             "24e472640390edb9a8e836f8d2010000006b483045022100b01e84b218e057d1714c40a9bdb4b3e851cc67d4d55e2be7130951959643d5c6022016b1c4028739" +
                             "180f5d4ee4b64b747cf85caf6137859a25d52d7f7ae58f63f2b3022102a79ddb507159bdcb918c1b5c659913eb699f73bb4258b8bc24586a5b896d6904ffffff" +
                             "ff0170170000000000001976a9144754ac901104098c23184986799ddd814dede65388ac00000000",
                inputIndex: 0,
                inputValue: 6000,
                inputScript: "76a9144754ac901104098c23184986799ddd814dede65388ac"
            );

            yield return new TransactionInputExample
            (
                name: "None",
                blockHeight: 357235,
                transaction: "0100000003ff067746e23b0994d86b06ff28799098c8631ddb0f9c9f84ec79fe03c5e5ea4c000000006b483045022100b518e5d0a1d8b30279bd4337c5be6c66" +
                             "5e9c42cb154cac9812e18b5abd3d4e6402205b86070023a339abfd2b0158143a42852ec6567ec87c32950eccbac282082e0f0221033978e80e30fe76867b11d1" +
                             "af3331ab0cd70e7a7242867e19b44ae3c64bda2adeffffffff4ef45c1d5a3243615baa50d53acafb5168544ad9175a32a3231114210d4e6161000000006b4830" +
                             "45022100e79bc6b1dd0bc01082a73b760795ce74c78398b11edbe97b2f6e5199afcc24e202200913b084841eed9551fc814e3453c99380d908e01e5d9a36b329" +
                             "d3adbfeb97280221020d13a5bb2a673f5e7e2bb21d1f50c58f34b8eeb59d6e782b802bbb8af4d234d5ffffffff0bb80b9719414ab6f674b756cdb1a573000d41" +
                             "24e472640390edb9a8e836f8d2010000006b483045022100b01e84b218e057d1714c40a9bdb4b3e851cc67d4d55e2be7130951959643d5c6022016b1c4028739" +
                             "180f5d4ee4b64b747cf85caf6137859a25d52d7f7ae58f63f2b3022102a79ddb507159bdcb918c1b5c659913eb699f73bb4258b8bc24586a5b896d6904ffffff" +
                             "ff0170170000000000001976a9144754ac901104098c23184986799ddd814dede65388ac00000000",
                inputIndex: 1,
                inputValue: 3389,
                inputScript: "76a914c68a9f86502a871b84e9ac99e081d6adeaa00ab088ac"
            );

            yield return new TransactionInputExample
            (
                name: "None, AnyoneCanPay",
                blockHeight: 260066,
                transaction: "010000000ab2910377d9d1b4fe14482af56ea4a9cd7435e20b4450968d7f707bb954533996010000006b483045022030f8a8fb0cc1e8d61b99647a5a89dc76b4c" +
                             "4f9e070b20e536a3e5418df68602d022100ee37d3e921cea0bf40aedcf0c9abe236b6f3e9a48f1b267782ab16839edd71cc81210309d71a4b034b3ff9b62bc064" +
                             "d97987ad7b4fe99fbc5a17921af304999f5a3506fffffffff56323588a66f96763281faaab8233702c1c9b6af9462376e8defffa5346c7d9010000006a4730440" +
                             "2204657c59c5672a858b79ec2924950d298a13bb2998af0fa195e1c53b3591bfe950220728d9446f05a44e1e983a49bc06eeb7373096f3223bfa7a81042a44395" +
                             "d87212822103de6731a39c8240b121a1e0527a0f763792e423a143bfddf0a0d96479f34a9ea5ffffffff32f136662790f6439f125070da037ac0636b2c926b2e7" +
                             "d8f1ccc0be237d272a0000000006b483045022100d0533830fe1df4ca19175fe6758e1df09c37f853c8419014208442c3cdb188f90220311cc0e3c27775e9538d" +
                             "e28b2155b6d3381b0180d07d91e5c010fab53a349e4e81210309d71a4b034b3ff9b62bc064d97987ad7b4fe99fbc5a17921af304999f5a3506ffffffff8d8574b" +
                             "ff3089c7926afc3edb4ebd8cdc1b36247f2e022d3c86e0abcb2cfc204000000006b48304502204ce70897cec62b3b33298f3f3523f8309eb8a91ca64e24facdbd" +
                             "7c7387f42105022100b02034a80aee40a4e6693bfabf5c84266acef85b4ef35560fc12023d8a574c58822103de6731a39c8240b121a1e0527a0f763792e423a14" +
                             "3bfddf0a0d96479f34a9ea5ffffffff744fa13487fa3c2bd56f29bdca15ad1726d9b689158b2994995ab29487cf19c8000000006a473044022065f13e3bf1eb38" +
                             "7031a74b5f06f13ea3925be51536a024e72daa19a0bbdc5a6102200ad361d2658f3a53e2f094d25cb4d3cf0aa59e224b666f92ba939dadba90087a822103de673" +
                             "1a39c8240b121a1e0527a0f763792e423a143bfddf0a0d96479f34a9ea5ffffffff6891f58baf300bae52287244df67e936ce7091374e6235344e85476f42d3fc" +
                             "72010000006c493046022100c8e9981294799d12471f14e82ac323026037297f93015693b2bc50105f3f3fb4022100b4ef802d08463b297b417f9becc1cbd0d3b" +
                             "a36e2927b19a975c21ba0f11a5b54822103de6731a39c8240b121a1e0527a0f763792e423a143bfddf0a0d96479f34a9ea5ffffffff51dfb74db7e7acef453bdc" +
                             "5ea556cc463f71874afaa6678040a31ba3e18d4cf8010000006b483045022100b3f4ad9e688ab25e41d14304f4d429683c05310bd32ba04d60897dc8923fd1980" +
                             "2202d3065c95d83cd57040c55fde151b0750db49b2c43f81e66c0f19465fd4e062e81210309d71a4b034b3ff9b62bc064d97987ad7b4fe99fbc5a17921af30499" +
                             "9f5a3506ffffffff427c9253ebbbbeb80589a30c3560e9b4b7f640678d3c982aebfb29fa35332286010000006b48304502210080729c067d2a694e2a38d133f5a" +
                             "abcb54776eb9eef85912975ac86bf8cac37b1022031728948b6e19dc1d5b4cbbbfbd56aa6daeb2ad5f3fb99835867fa086cd4d968822103de6731a39c8240b121" +
                             "a1e0527a0f763792e423a143bfddf0a0d96479f34a9ea5ffffffff31c9001b0b9e7130bd48d9c59e392a5aad1eb6b0ddd1dee057d2f3c961812b18000000006c4" +
                             "93046022100b9b99adcd4a39db7805dc8a0f33ca94689782dd00af39ab952dc84ce4836902a0221008b0ae3d1bf76a07da52c17206424ce03a4126068d74c16cb" +
                             "5717c5755779f534822103de6731a39c8240b121a1e0527a0f763792e423a143bfddf0a0d96479f34a9ea5ffffffffba122690f01ba9daf19b946cf7c0ea6b049" +
                             "27a5e7af7f70d100dba372a7f7baf000000006a47304402204472df52e1454af343429a48dd769a8ef7486f90dc1f22b9c2d82ef5469076dc02200454c7371416" +
                             "405c85c8bbc53a3842741b0eec5fdd90808786d6a4bb4ec83b26822103de6731a39c8240b121a1e0527a0f763792e423a143bfddf0a0d96479f34a9ea5fffffff" +
                             "f010000000000000000016a00000000",
                inputIndex: 1,
                inputValue: 10000,
                inputScript: "76a914e087288fea1ad076c5215391416eb65ec9a9140c88ac"
            );

            yield return new TransactionInputExample
            (
                name: "None, AnyoneCanPay",
                blockHeight: 299255,
                transaction: "0100000001a136731e1a17b46f423bcfac3f7760aad7cd993b4b29ba9285892ad33cf4df66010000006b48304502202a1e12d52ed31e5b80cc8bbd08d3c1e41a" +
                             "f12b5fe5cca88f5f304d6a7606a218022100e936e7066eed52fff4ab6aa9c9e6a1f5d61509d5e0fe0aa9e8f32663badc1fb1822103a34b99f22c790c4e36b2b3" +
                             "c2c35a36db06226e41c692fc82b8b56ac1c540c5bdffffffff010000000000000000016a00000000",
                inputIndex: 0,
                inputValue: 10000,
                inputScript: "76a9149a1c78a507689f6f54b847ad1cef1e614ee23f1e88ac"
            );

            yield return new TransactionInputExample
            (
                name: "None, AnyoneCanPay",
                blockHeight: 357564,
                transaction: "0100000002cbdd3374888edbdef345ba81c5684081ead12aa99938183678f6e0585858b898000000006b483045022100d385f644e11fe5594d25ff81ad1b09a2" +
                             "a48e64203768b526d5d2c866306e809e02204017cf4a4b9a7b3dc3bcde17727670f0923fe02c8d180e70948dc17ce66d6b6a822103e6d5bfc64252a18a104a47" +
                             "85b4b7502fd148f26524ce884cb97fcb178459cb85ffffffff795fbf6f588f1129bfe8a2609928d5f4943bcf55ad3374d540f4bc728ba91b69010000006b4830" +
                             "45022100f0a49585994b358e41b43c479eb9667d048a1d94cf9eefd0a90dd4c110f4912602203800ff216f5d3149ca094714f32cbee0b7bd2bd0f8add85b424e" +
                             "409fb808ba7d822103479a1ecbce4df83b8d2a1d13d5bc6238207a3344aff513bebd23f2a14c958c13ffffffff010000000000000000016a00000000",
                inputIndex: 1,
                inputValue: 8220,
                inputScript: "76a914ad8d906e150a02abbd72a7ebeb741f27ce1448ed88ac"
            );

            yield return new TransactionInputExample
            (
                name: "Single",
                blockHeight: 233999,
                transaction: "0100000001285fcaca252ed0d285d1037b286b91e14fd9143c620845cb0493f69a03c50b19000000008b483045022100a36ead3f36c5f1a0fadb556d051ee9bb5" +
                             "dc65b6b7d543dd4c87b5579cd92528a02203153050e3d9e78b1d45792ae8b86c7bd9a8fa19353951e8eaab80029a5b8f6a00341046c3614bdec555c8c7cdf3e69" +
                             "d94605eb98ec9640173f2ad5826f5db0f6bf21f7b2e8fbdf7d24eee63d384cfe7a4f8e8ca9ea4d4de4f0e674c21413b6a8028defffffffff01a86100000000000" +
                             "01976a914425eba3205e51d597cf9152ca835e3a889fac11a88ac00000000",
                inputIndex: 0,
                inputValue: 16515733,
                inputScript: "76a914425eba3205e51d597cf9152ca835e3a889fac11a88ac"
            );

            yield return new TransactionInputExample
            (
                name: "Single",
                blockHeight: 238797,
                transaction: "010000000370ac0a1ae588aaf284c308d67ca92c69a39e2db81337e563bf40c59da0a5cf63000000006a4730440220360d20baff382059040ba9be98947fd678f" +
                             "b08aab2bb0c172efa996fd8ece9b702201b4fb0de67f015c90e7ac8a193aeab486a1f587e0f54d0fb9552ef7f5ce6caec032103579ca2e6d107522f012cd00b52" +
                             "b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff7d815b6447e35fbea097e00e028fb7dfbad4f3f0987b4734676c84f3fcd0e804010000006b483045022" +
                             "100c714310be1e3a9ff1c5f7cacc65c2d8e781fc3a88ceb063c6153bf950650802102200b2d0979c76e12bb480da635f192cc8dc6f905380dd4ac1ff35a4f68f4" +
                             "62fffd032103579ca2e6d107522f012cd00b52b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff3f1f097333e4d46d51f5e77b53264db8f7f5d2e18217e" +
                             "1099957d0f5af7713ee010000006c493046022100b663499ef73273a3788dea342717c2640ac43c5a1cf862c9e09b206fcb3f6bb8022100b09972e75972d9148f" +
                             "2bdd462e5cb69b57c1214b88fc55ca638676c07cfc10d8032103579ca2e6d107522f012cd00b52b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff03808" +
                             "41e00000000001976a914bfb282c70c4191f45b5a6665cad1682f2c9cfdfb88ac80841e00000000001976a9149857cc07bed33a5cf12b9c5e0500b675d500c811" +
                             "88ace0fd1c00000000001976a91443c52850606c872403c0601e69fa34b26f62db4a88ac00000000",
                inputIndex: 0,
                inputValue: 2000000,
                inputScript: "76a914dcf72c4fd02f5a987cf9b02f2fabfcac3341a87d88ac"
            );

            yield return new TransactionInputExample
            (
                name: "Single",
                blockHeight: 238797,
                transaction: "010000000370ac0a1ae588aaf284c308d67ca92c69a39e2db81337e563bf40c59da0a5cf63000000006a4730440220360d20baff382059040ba9be98947fd678f" +
                             "b08aab2bb0c172efa996fd8ece9b702201b4fb0de67f015c90e7ac8a193aeab486a1f587e0f54d0fb9552ef7f5ce6caec032103579ca2e6d107522f012cd00b52" +
                             "b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff7d815b6447e35fbea097e00e028fb7dfbad4f3f0987b4734676c84f3fcd0e804010000006b483045022" +
                             "100c714310be1e3a9ff1c5f7cacc65c2d8e781fc3a88ceb063c6153bf950650802102200b2d0979c76e12bb480da635f192cc8dc6f905380dd4ac1ff35a4f68f4" +
                             "62fffd032103579ca2e6d107522f012cd00b52b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff3f1f097333e4d46d51f5e77b53264db8f7f5d2e18217e" +
                             "1099957d0f5af7713ee010000006c493046022100b663499ef73273a3788dea342717c2640ac43c5a1cf862c9e09b206fcb3f6bb8022100b09972e75972d9148f" +
                             "2bdd462e5cb69b57c1214b88fc55ca638676c07cfc10d8032103579ca2e6d107522f012cd00b52b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff03808" +
                             "41e00000000001976a914bfb282c70c4191f45b5a6665cad1682f2c9cfdfb88ac80841e00000000001976a9149857cc07bed33a5cf12b9c5e0500b675d500c811" +
                             "88ace0fd1c00000000001976a91443c52850606c872403c0601e69fa34b26f62db4a88ac00000000",
                inputIndex: 1,
                inputValue: 2000000,
                inputScript: "76a914dcf72c4fd02f5a987cf9b02f2fabfcac3341a87d88ac"
            );

            yield return new TransactionInputExample
            (
                name: "Single",
                blockHeight: 238797,
                transaction: "010000000370ac0a1ae588aaf284c308d67ca92c69a39e2db81337e563bf40c59da0a5cf63000000006a4730440220360d20baff382059040ba9be98947fd678f" +
                             "b08aab2bb0c172efa996fd8ece9b702201b4fb0de67f015c90e7ac8a193aeab486a1f587e0f54d0fb9552ef7f5ce6caec032103579ca2e6d107522f012cd00b52" +
                             "b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff7d815b6447e35fbea097e00e028fb7dfbad4f3f0987b4734676c84f3fcd0e804010000006b483045022" +
                             "100c714310be1e3a9ff1c5f7cacc65c2d8e781fc3a88ceb063c6153bf950650802102200b2d0979c76e12bb480da635f192cc8dc6f905380dd4ac1ff35a4f68f4" +
                             "62fffd032103579ca2e6d107522f012cd00b52b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff3f1f097333e4d46d51f5e77b53264db8f7f5d2e18217e" +
                             "1099957d0f5af7713ee010000006c493046022100b663499ef73273a3788dea342717c2640ac43c5a1cf862c9e09b206fcb3f6bb8022100b09972e75972d9148f" +
                             "2bdd462e5cb69b57c1214b88fc55ca638676c07cfc10d8032103579ca2e6d107522f012cd00b52b9a65fb46f0c57b9b8b6e377c48f526a44741affffffff03808" +
                             "41e00000000001976a914bfb282c70c4191f45b5a6665cad1682f2c9cfdfb88ac80841e00000000001976a9149857cc07bed33a5cf12b9c5e0500b675d500c811" +
                             "88ace0fd1c00000000001976a91443c52850606c872403c0601e69fa34b26f62db4a88ac00000000",
                inputIndex: 2,
                inputValue: 2000000,
                inputScript: "76a914dcf72c4fd02f5a987cf9b02f2fabfcac3341a87d88ac"
            );

            yield return new TransactionInputExample
            (
                name: "Single",
                blockHeight: 247939,
                transaction: "0100000002dc38e9359bd7da3b58386204e186d9408685f427f5e513666db735aa8a6b2169000000006a47304402205d8feeb312478e468d0b514e63e113958d7" +
                             "214fa572acd87079a7f0cc026fc5c02200fa76ea05bf243af6d0f9177f241caf606d01fcfd5e62d6befbca24e569e5c27032102100a1a9ca2c18932d6577c58f2" +
                             "25580184d0e08226d41959874ac963e3c1b2feffffffffdc38e9359bd7da3b58386204e186d9408685f427f5e513666db735aa8a6b2169010000006b483045022" +
                             "0087ede38729e6d35e4f515505018e659222031273b7366920f393ee3ab17bc1e022100ca43164b757d1a6d1235f13200d4b5f76dd8fda4ec9fc28546b2df5b12" +
                             "11e8df03210275983913e60093b767e85597ca9397fb2f418e57f998d6afbbc536116085b1cbffffffff0140899500000000001976a914fcc9b36d38cf55d7d5b" +
                             "4ee4dddb6b2c17612f48c88ac00000000",
                inputIndex: 0,
                inputValue: 4950000,
                inputScript: "76a914fcc9b36d38cf55d7d5b4ee4dddb6b2c17612f48c88ac"
            );

            yield return new TransactionInputExample
            (
                name: "Single, AnyoneCanPay",
                blockHeight: 300992,
                transaction: "010000000250d7d66f69f0b61a6d3e808707e441be0623493912b7893723b7befb3fece044000000006b483045022100bbf1696e71c1cf2b837d50b7c1218a59" +
                             "d61a1f97ea96467a67b2e01d053922ff0220665ab4873f7edc43a19571bd9c131fcd72ec60f172ee1ff0a4f18e69a9d8bbfd83210252ce4bdd3ce38b4ebbc5a6" +
                             "e1343608230da508ff12d23d85b58c964204c4cef3fffffffff178b861a99b6f6d5417688d107cc0e6d9ccba957bb8ec071dc4aa2ea619adfd000000006a4730" +
                             "440220368784ac516dab015a7191edca16c593e4af029f8b80356a6245eab4bf03247b022054d99ee0a005759691527c350e58c12e52f637c861710577b1ba2d" +
                             "8c04ceb5b501210329f1360be71c7d988f6d298f41a31b0ca822478dcf570b28a5d8a81002d233d3ffffffff02e4240100000000001976a914d84bcec5b65aa1" +
                             "a03d6abfd975824c75856a296188ac00000000000000000f6a0d73696e676c652b616e796f6e6500000000",
                inputIndex: 0,
                inputValue: 79520,
                inputScript: "76a914d84bcec5b65aa1a03d6abfd975824c75856a296188ac"
            );
        }

        [Test]
        public void TestSingleHistoricalExamples([ValueSource(nameof(GetSingleHistoricalExamples))] TransactionInputExample example)
        {
            // Note: The transaction that uses SIGHASH_SINGLE type of signature should not have more inputs than outputs.
            // However if it does (because of the pre-existing implementation), it shall not be rejected,
            // but instead for every "illegal" input (meaning: an input that has an index bigger than the maximum output index) the node should still verify it,
            // though assuming the hash of 0000000000000000000000000000000000000000000000000000000000000001

            ISigHashCalculator sigHashCalculator = new BitcoinCoreSigHashCalculator(example.Transaction);
            sigHashCalculator.InputIndex = example.InputIndex;
            sigHashCalculator.Amount = example.InputValue;

            ScriptProcessor scriptProcessor = new ScriptProcessor();
            scriptProcessor.SigHashCalculator = sigHashCalculator;

            scriptProcessor.Execute(example.Transaction.Inputs[example.InputIndex].SignatureScript);

            Assert.That
            (
                () => scriptProcessor.Execute(example.InputScript),
                Throws.Exception.TypeOf<InvalidOperationException>().With.Message.ContainsSubstring("SIGHASH_SINGLE")
            );
        }

        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        private static IEnumerable<TransactionInputExample> GetSingleHistoricalExamples()
        {
            yield return new TransactionInputExample
            (
                name: "Single (input index greater than last output index)",
                blockHeight: 247939,
                transaction: "0100000002dc38e9359bd7da3b58386204e186d9408685f427f5e513666db735aa8a6b2169000000006a47304402205d8feeb312478e468d0b514e63e113958d7" +
                             "214fa572acd87079a7f0cc026fc5c02200fa76ea05bf243af6d0f9177f241caf606d01fcfd5e62d6befbca24e569e5c27032102100a1a9ca2c18932d6577c58f2" +
                             "25580184d0e08226d41959874ac963e3c1b2feffffffffdc38e9359bd7da3b58386204e186d9408685f427f5e513666db735aa8a6b2169010000006b483045022" +
                             "0087ede38729e6d35e4f515505018e659222031273b7366920f393ee3ab17bc1e022100ca43164b757d1a6d1235f13200d4b5f76dd8fda4ec9fc28546b2df5b12" +
                             "11e8df03210275983913e60093b767e85597ca9397fb2f418e57f998d6afbbc536116085b1cbffffffff0140899500000000001976a914fcc9b36d38cf55d7d5b" +
                             "4ee4dddb6b2c17612f48c88ac00000000",
                inputIndex: 1,
                inputValue: 4950000,
                inputScript: "76a91433cef61749d11ba2adf091a5e045678177fe3a6d88ac"
            );

            yield return new TransactionInputExample
            (
                name: "Single (input index greater than last output index)",
                blockHeight: 290333,
                transaction: "0100000002dbb33bdf185b17f758af243c5d3c6e164cc873f6bb9f40c0677d6e0f8ee5afce000000006b4830450221009627444320dc5ef8d7f68f35010b4c050" +
                             "a6ed0d96b67a84db99fda9c9de58b1e02203e4b4aaa019e012e65d69b487fdf8719df72f488fa91506a80c49a33929f1fd50121022b78b756e2258af13779c1a1" +
                             "f37ea6800259716ca4b7f0b87610e0bf3ab52a01ffffffffdbb33bdf185b17f758af243c5d3c6e164cc873f6bb9f40c0677d6e0f8ee5afce01000000930048304" +
                             "5022015bd0139bcccf990a6af6ec5c1c52ed8222e03a0d51c334df139968525d2fcd20221009f9efe325476eb64c3958e4713e9eefe49bf1d820ed58d2112721b" +
                             "134e2a1a5303483045022015bd0139bcccf990a6af6ec5c1c52ed8222e03a0d51c334df139968525d2fcd20221009f9efe325476eb64c3958e4713e9eefe49bf1" +
                             "d820ed58d2112721b134e2a1a5303ffffffff01a0860100000000001976a9149bc0bbdd3024da4d0c38ed1aecf5c68dd1d3fa1288ac00000000",
                inputIndex: 1,
                inputValue: 100000,
                inputScript: "52483045022015bd0139bcccf990a6af6ec5c1c52ed8222e03a0d51c334df139968525d2fcd20221009f9efe325476eb64c3958e4713e9eefe49bf1d820ed58d2" +
                             "112721b134e2a1a5303210378d430274f8c5ec1321338151e9f27f4c676a008bdf8638d07c0b6be9ab35c71210378d430274f8c5ec1321338151e9f27f4c676a0" +
                             "08bdf8638d07c0b6be9ab35c7153ae"
            );
        }
    }
}