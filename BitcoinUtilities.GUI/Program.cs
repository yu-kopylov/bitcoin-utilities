using System;
using BitcoinUtilities.GUI.Models;
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
            Application application = new Application(Platform.Detect);

            MainForm mainForm = new MainForm();

            ApplicationContext applicationContext = new ApplicationContext();
            IViewContext viewContext = new ViewContext(mainForm);

            mainForm.DataContext = new MainFormViewModel(applicationContext, viewContext);

            application.Run(mainForm);
        }
    }
}