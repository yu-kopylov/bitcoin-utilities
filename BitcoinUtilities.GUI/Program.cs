using System;
using BitcoinUtilities.GUI.Views;
using Eto;
using Eto.Forms;

namespace BitcoinUtilities.GUI
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            new Application(Platform.Detect).Run(new MainForm());
        }
    }
}
