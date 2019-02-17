using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using ZXing;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class PaperWalletViewModel : INotifyPropertyChanged
    {
        private readonly IViewContext viewContext;

        //todo: add common timer for delayed UI events
        private readonly Timer timer;
        private volatile bool inputUpdated = true;
        private volatile bool inputUpdatedRecently;

        private string password = "test";
        private string privateKey = "KwdMAjGmerYanjeui5SHS7JkmpZvVipYvB2LJGU1ZxJwYvP98617";
        private byte[] qrCodeImage;
        private string encryptedKey;

        public PaperWalletViewModel(IViewContext viewContext)
        {
            this.viewContext = viewContext;
            timer = new Timer(TimerTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(300));
        }

        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                OnPropertyChanged();

                inputUpdated = true;
                inputUpdatedRecently = true;
            }
        }

        public string PrivateKey
        {
            get { return privateKey; }
            set
            {
                privateKey = value;
                OnPropertyChanged();

                inputUpdated = true;
                inputUpdatedRecently = true;
            }
        }

        public byte[] QrCodeImage
        {
            get { return qrCodeImage; }
            private set
            {
                qrCodeImage = value;
                OnPropertyChanged();
            }
        }

        public string EncryptedKey
        {
            get { return encryptedKey; }
            set
            {
                encryptedKey = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void TimerTick(object state)
        {
            if (inputUpdated && !inputUpdatedRecently)
            {
                inputUpdated = false;
                UpdateImage();
            }

            inputUpdatedRecently = false;
        }

        private void UpdateImage()
        {
            viewContext.Invoke(() =>
            {
                EncryptedKey = null;
                QrCodeImage = null;
            });

            if (!Wif.TryDecode(BitcoinNetworkKind.Main, privateKey, out var privateKeyBytes, out var compressed))
            {
                return;
            }

            string encryptedKey;
            try
            {
                encryptedKey = Bip38.Encrypt(privateKeyBytes, password, compressed);
            }
            catch (ArgumentException)
            {
                return;
            }

            IBarcodeWriter writer = new BarcodeWriter {Format = BarcodeFormat.QR_CODE};

            var mem = new MemoryStream();

            using (System.Drawing.Bitmap bitmap = writer.Write(encryptedKey))
            {
                bitmap.Save(mem, System.Drawing.Imaging.ImageFormat.Png);
            }

            byte[] bitmapBytes = mem.ToArray();

            viewContext.Invoke(() =>
            {
                EncryptedKey = encryptedKey;
                QrCodeImage = bitmapBytes;
            });
        }
    }
}