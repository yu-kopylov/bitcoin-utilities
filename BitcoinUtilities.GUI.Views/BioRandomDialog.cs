using System;
using BitcoinUtilities.GUI.Views.Components;
using Eto.Drawing;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views
{
    public class BioRandomDialog : Dialog
    {
        /// <summary>
        /// The minimum bits of entropy of the generated seed material.
        /// </summary>
        public const int TargetEntropy = 512;

        private readonly BioRandom random = new BioRandom();
        private readonly Label captionLabel = new Label {Text = "Move your mouse as random as possible within this window to generate a random value."};
        private readonly ProgressBar progressBar = new ProgressBar {MinValue = 0, MaxValue = 100};

        /// <summary>
        /// Creates a new instance of a dialog.
        /// </summary>
        public BioRandomDialog()
        {
            Title = "Random Generator";
            Width = 400;
            Height = 400;
            MouseMove += OnMouseMove;

            var buttonsPanel = new ButtonsPanel();
            AbortButton = buttonsPanel.AddButton("Cancel", Close);

            var mainTable = new TableLayout {Padding = 10, Spacing = new Size(5, 5)};
            mainTable.Rows.Add(new TableRow(captionLabel) {ScaleHeight = true});
            mainTable.Rows.Add(new TableRow(progressBar));
            mainTable.Rows.Add(new TableRow(buttonsPanel));

            Content = mainTable;
        }

        /// <summary>
        /// The generated seed material.
        /// <para/>
        /// Contains null if the generation process was not finished.
        /// </summary>
        public byte[] SeedMaterial { get; private set; }

        private void OnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var location = mouseEventArgs.Location;
            random.AddPoint(location.X, location.Y);
            progressBar.Value = Math.Min(random.Entropy * 100 / TargetEntropy, 100);
            if (random.Entropy >= TargetEntropy)
            {
                SeedMaterial = random.CreateSeedMaterial();
                Close();
            }
        }
    }
}