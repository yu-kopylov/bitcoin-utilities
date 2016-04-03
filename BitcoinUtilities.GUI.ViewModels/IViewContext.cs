using System;

namespace BitcoinUtilities.GUI.ViewModels
{
    public interface IViewContext
    {
        void ShowError(Exception e);
    }
}