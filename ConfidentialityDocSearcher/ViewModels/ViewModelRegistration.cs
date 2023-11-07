using ConfidentialityDocSearcher.ViewModels.MainWindowVm;
using Microsoft.Extensions.DependencyInjection;

namespace ConfidentialityDocSearcher.ViewModels
{
    public static class ViewModelRegistration
    {
        public static IServiceCollection RegisterViewModels(this IServiceCollection services)
        {
            services.AddSingleton<MainWindowViewModel>();
            return services;
        }
    }
}