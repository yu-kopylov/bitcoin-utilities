using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;
using ZXing;

namespace BitcoinUtilities.GUI.Views
{
    public class Bip38EncoderDialog : Dialog, INotifyPropertyChanged
    {
        private readonly Timer timer;
        private volatile bool inputUpdated = true;
        private volatile bool inputUpdatedRecently;

        private string password = "test";
        private string privateKey = "KwdMAjGmerYanjeui5SHS7JkmpZvVipYvB2LJGU1ZxJwYvP98617";
        private Image qrCodeImage;

        public Bip38EncoderDialog()
        {
            XamlReader.Load(this);

            DataContext = this;

            timer = new Timer(TimerTick, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(300));

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                if (qrCodeImage != null)
                {
                    qrCodeImage.Dispose();
                    qrCodeImage = null;
                }
            }
            base.Dispose(disposing);
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

        public Image QrCodeImage
        {
            get { return qrCodeImage; }
            set
            {
                qrCodeImage = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
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
            if (QrCodeImage != null)
            {
                Application.Instance.Invoke(() =>
                {
                    QrCodeImage.Dispose();
                    QrCodeImage = null;
                });
            }

            byte[] privateKeyBytes;
            bool compressed;

            if (!Wif.Decode(privateKey, out privateKeyBytes, out compressed))
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

            IBarcodeWriter writer = new BarcodeWriter { Format = BarcodeFormat.QR_CODE };

            var mem = new MemoryStream();

            //todo: move image generation to ViewModel and remove reference to System.Drawing from this project
            using (System.Drawing.Bitmap bitmap = writer.Write(encryptedKey))
            {
                bitmap.Save(mem, System.Drawing.Imaging.ImageFormat.Png);
            }

            mem.Position = 0;

            Application.Instance.Invoke(() => { QrCodeImage = new Bitmap(mem); });
        }

    }
}
