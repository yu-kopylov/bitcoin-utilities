using System;
using BitcoinUtilities.GUI.ViewModels;
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
            ApplicationContext applicationContext = new ApplicationContext();
            new Application(Platform.Detect).Run(new MainForm(applicationContext));
        }
    }
}
