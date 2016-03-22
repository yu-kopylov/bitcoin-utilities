using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.Forms
{
    /// <summary>
    /// The dialog for a seed material generation based on mouse movement.
    /// </summary>
    public class BioRandomDialog : Dialog, INotifyPropertyChanged
    {
        /// <summary>
        /// The minimum bits of entropy of the generated seed material.
        /// </summary>
        public const int TargetEntropy = 512;

        private readonly BioRandom random = new BioRandom();

        private int progress;
        private byte[] seedMeterial;

        /// <summary>
        /// Creates a new instance of a dialog.
        /// </summary>
        public BioRandomDialog()
        {
            XamlReader.Load(this);

            DataContext = this;

            MouseMove += OnMouseMove;
        }

        /// <summary>
        /// The percent of completion.
        /// </summary>
        public int Progress
        {
            get { return progress; }
            private set
            {
                progress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The generated seed material.
        /// <para/>
        /// Contains null if the generation process was not finished.
        /// </summary>
        public byte[] SeedMeterial
        {
            get { return seedMeterial; }
            private set
            {
                seedMeterial = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnMouseMove(object sender, MouseEventArgs mouseEventArgs)
        {
            var location = mouseEventArgs.Location;
            random.AddPoint(location.X, location.Y);
            Progress = Math.Min(random.Entropy*100/TargetEntropy, 100);
            if (random.Entropy >= TargetEntropy)
            {
                SeedMeterial = random.CreateSeedMaterial();
                Close();
            }
        }

        protected void AbortButton_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}