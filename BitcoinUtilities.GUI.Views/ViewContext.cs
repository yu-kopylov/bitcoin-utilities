using System;
using BitcoinUtilities.GUI.ViewModels;
using Eto.Forms;

namespace BitcoinUtilities.GUI.Views
{
    public class ViewContext : IViewContext
    {
        private readonly Window window;

        public ViewContext(Window window)
        {
            this.window = window;
        }

        public void ShowError(Exception e)
        {
            MessageBox.Show(window, e.Message, MessageBoxType.Error);
        }

        public void Invoke(Action action)
        {
            Application.Instance.Invoke(action);
        }
    }
}