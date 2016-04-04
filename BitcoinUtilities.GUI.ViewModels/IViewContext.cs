using System;

namespace BitcoinUtilities.GUI.ViewModels
{
    public interface IViewContext
    {
        /// <summary>
        /// Displays information about an exception.
        /// </summary>
        /// <param name="e">The exception.</param>
        void ShowError(Exception e);

        /// <summary>
        /// Perform an action in the UI thread.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        void Invoke(Action action);
    }
}