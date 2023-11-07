using ConfidentialityDocSearcher.Views;
using System;
using System.Threading;
using System.Windows;

namespace ConfidentialityDocSearcher.Service.UserDialogService
{
    internal class WindowsUserDialogService : IUserDialogService
    {
        private static Window ActiveWindow => Application.Current.MainWindow; //.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);

        public void ShowInformation(string message) => MessageBox
            .Show(ActiveWindow, message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        public void ShowWarning(string message) => ShowErrorWindow(message, ErrorWindowType.Warning);
        public void ShowError(string message) => ShowErrorWindow(message, ErrorWindowType.Error);

        private void ShowErrorWindow(string msg, ErrorWindowType type)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var dlg = new ErrorWindow(msg, type)
                {
                    Owner = ActiveWindow
                };
                dlg.ShowDialog();
            });
        }

        public bool Confirm(string message, bool exclamation = false) => MessageBox
            .Show(message, "Запрос пользователю", MessageBoxButton.YesNo,
                exclamation ? MessageBoxImage.Exclamation : MessageBoxImage.Question) == MessageBoxResult.Yes;

        public (IProgress<double> Progress, IProgress<string> Status, CancellationToken Cancel, Action Close) ShowProgress(string title)
        {
            var progressWindow = new ProgressWindow { Title = title, Owner = ActiveWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            progressWindow.Show();
            return (progressWindow.ProgressInformer, progressWindow.StatusInformer, progressWindow.Cancellation, progressWindow.Close);
        }
    }
}