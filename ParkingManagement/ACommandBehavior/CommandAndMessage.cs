using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows;

namespace ParkingManagement.ACommandBehavior
{
    #region Class Command
    public class MyCommand : ICommand
    {
        private Action<string> _CodeToExecute;
        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;


        void ICommand.Execute(object parameter)
        {
            _CodeToExecute(null);
        }
        public MyCommand(Action<string> CodeToExecute)
        {
            _CodeToExecute = CodeToExecute;
        }
    }

    #endregion


    #region Class Message
        public interface IMessageBoxService
    {
        System.Windows.MessageBoxResult Show(string messageBoxText, string caption = null, System.Windows.MessageBoxButton buttons = System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage icon = System.Windows.MessageBoxImage.None, System.Windows.MessageBoxResult defaultResult = System.Windows.MessageBoxResult.None, System.Windows.MessageBoxOptions options = System.Windows.MessageBoxOptions.None);
        System.Windows.MessageBoxResult ShowException(System.Exception exception, string caption = null);
        System.Windows.MessageBoxResult ShowError(string messageBoxText, string caption = null);
        System.Windows.MessageBoxResult ShowWarning(string messageBoxText, string caption = null);
        System.Windows.MessageBoxResult ShowInformation(string messageBoxText, string caption = null);
    }

    public class MessageBoxService : IMessageBoxService
    {

        MessageBoxResult IMessageBoxService.Show(string messageBoxText, string caption, MessageBoxButton buttons, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            MessageBoxResult Result = MessageBox.Show(messageBoxText, caption, buttons, icon, defaultResult, options);
            return Result;
            //throw new NotImplementedException();
        }

        MessageBoxResult IMessageBoxService.ShowException(Exception exception, string caption)
        {
            throw new NotImplementedException();
        }

        MessageBoxResult IMessageBoxService.ShowError(string messageBoxText, string caption)
        {
            MessageBox.Show(messageBoxText, caption);
            return MessageBoxResult.OK;
            //throw new NotImplementedException();
        }

        MessageBoxResult IMessageBoxService.ShowWarning(string messageBoxText, string caption)
        {
            throw new NotImplementedException();
        }

        MessageBoxResult IMessageBoxService.ShowInformation(string messageBoxText, string caption)
        {
            throw new NotImplementedException();
        }
    }

    

    #endregion
}
