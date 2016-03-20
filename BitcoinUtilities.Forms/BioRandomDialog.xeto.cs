using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.Forms
{
    /// <summary>
    /// The dialog for a random value generation.
    /// </summary>
    public class BioRandomDialog : Dialog, INotifyPropertyChanged
    {
        private readonly int length;
        //todo: reset somewhere?
        private readonly BioRandom random = new BioRandom();

        private int progress;
        private byte[] randomValue;

        /// <summary>
        /// Creates a new instance of a dialog.
        /// </summary>
        /// <param name="length">Required length of the random value in bytes. Must be in range from 1 to 64.</param>
        public BioRandomDialog(int length)
        {
            if (length < 1 || length > 64)
            {
                throw new ArgumentException("Length must be in range from 1 to 64.", nameof(length));
            }

            this.length = length;
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
        /// The generated random value.
        /// <para/>
        /// Contains null if the generation process was cancelled.
        /// </summary>
        public byte[] RandomValue
        {
            get { return randomValue; }
            private set
            {
                randomValue = value;
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
            Progress = random.Entropy;
            if (Progress >= 100)
            {
                RandomValue = random.CreateValue(length);
                Close();
            }
        }

        protected void AbortButton_Click(object sender, EventArgs e)
        {
            Close();
        }

    }
}
