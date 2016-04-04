using System;
using BitcoinUtilities.GUI.ViewModels;
using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.GUI.Views
{
    public class PaperWalletView : Panel
    {
        protected ImageView QrCodeImage;

        public PaperWalletView()
        {
            XamlReader.Load(this);

            IndirectBinding<Image> imageBinding = Binding.Property((PaperWalletViewModel d) => d.QrCodeImage).Convert(ToImage, null);
            QrCodeImage.BindDataContext(Binding.Property((ImageView c) => c.Image), imageBinding);
        }

        private static Image ToImage(byte[] bitmapBytes)
        {
            if (bitmapBytes == null)
            {
                return null;
            }
            return new Bitmap(bitmapBytes);
        }
    }
}