using ConfidentialityDocSearcher.Service.UserDialogService;
using Microsoft.Extensions.DependencyInjection;

namespace ConfidentialityDocSearcher.Service
{
    public static class ServiceRegistration
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddTransient<IUserDialogService, WindowsUserDialogService>();
            return services;
        }
    }
}