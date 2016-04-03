using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BitcoinUtilities.GUI.ViewModels
{
    public class BitcoinNodeViewModel : INotifyPropertyChanged
    {
        private BitcoinNode node;
        private string state;
        private int connectionCount;

        public BitcoinNodeViewModel()
        {
            UpdateValues();
        }

        public BitcoinNode Node
        {
            get { return node; }
            set
            {
                if (node != null)
                {
                    node.PropertyChanged -= OnNodePropertyChanged;
                }
                node = value;
                node.PropertyChanged += OnNodePropertyChanged;
                UpdateValues();
                OnPropertyChanged();
            }
        }

        public string State
        {
            get { return state; }
            set
            {
                state = value;
                OnPropertyChanged();
            }
        }

        public int ConnectionCount
        {
            get { return connectionCount; }
            set
            {
                connectionCount = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnNodePropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            UpdateValues();
        }

        private void UpdateValues()
        {
            if (node == null)
            {
                State = "No Node";
                ConnectionCount = 0;
                return;
            }
            State = node.Started ? "Started" : "Stopped";
            ConnectionCount = node.Endpoints.Count;
        }
    }
}