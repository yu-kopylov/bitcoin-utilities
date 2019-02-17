using BitcoinUtilities.GUI.ViewModels;
using BitcoinUtilities.GUI.Views.Components;
using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views
{
    public class PaperWalletView : Panel
    {
        private static readonly Bitmap emptyBitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb);
        private readonly ImageView qrCodeImage = new ImageView {Width = 350, Height = 350};

        public PaperWalletView()
        {
            var propertiesTable = new PropertiesTable();
            propertiesTable.AddTextBox("Private Key (WIF)", nameof(PaperWalletViewModel.PrivateKey));
            propertiesTable.AddPasswordBox("Password", nameof(PaperWalletViewModel.Password));
            propertiesTable.AddLabel("Encrypted Key", nameof(PaperWalletViewModel.EncryptedKey));

            IndirectBinding<Image> imageBinding = Binding.Property<byte[]>(nameof(PaperWalletViewModel.QrCodeImage)).Convert(ToImage);
            qrCodeImage.BindDataContext(Binding.Property<Image>(nameof(ImageView.Image)), imageBinding);

            var mainTable = new TableLayout();
            mainTable.Rows.Add(new PaddedPanel(propertiesTable));
            mainTable.Rows.Add(new HorizontalDivider());
            mainTable.Rows.Add(new TableRow(new TableCell(new PaddedPanel(qrCodeImage) {BackgroundColor = SystemColors.Control})) {ScaleHeight = true});

            Content = mainTable;
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