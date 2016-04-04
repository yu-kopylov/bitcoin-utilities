using BitcoinUtilities.GUI.ViewModels;
using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.GUI.Views
{
    public class PaperWalletView : Panel
    {
        private static readonly Bitmap emptyBitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb);

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
                //todo: ImageView from Eto.Forms 2.2.0 crashes with null image in GTK3 (should be fixed in next release)
                return emptyBitmap;
            }
            return new Bitmap(bitmapBytes);
        }
    }
}