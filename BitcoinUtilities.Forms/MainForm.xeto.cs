﻿using System.Windows.Input;
using Eto.Forms;
using Eto.Serialization.Xaml;

namespace BitcoinUtilities.Forms
{
    public class MainForm : Form
    {
        public MainForm()
        {
            XamlReader.Load(this);

            DataContext = this;
        }

        public ICommand ShowBip38Encoder
        {
            get { return new Command((sender, e) => MessageBox.Show("ShowBip38Encoder Clicked!")); }
        }
    }
}
