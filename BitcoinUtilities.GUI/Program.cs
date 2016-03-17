using System;
using BitcoinUtilities.Forms;
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
