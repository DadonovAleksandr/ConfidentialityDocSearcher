using ConfidentialityDocSearcher.ViewModels.MainWindowVm;
using Microsoft.Extensions.DependencyInjection;

namespace ConfidentialityDocSearcher.ViewModels
{
    internal class ViewModelLocator
    {
        public MainWindowViewModel MainWindowViewModel => App.Host.Services.GetRequiredService<MainWindowViewModel>();
    }
}