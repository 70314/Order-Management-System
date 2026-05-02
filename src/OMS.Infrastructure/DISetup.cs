using Microsoft.Extensions.DependencyInjection;
using OMS.Domain.Interfaces;

namespace OMS.Infrastructure {
    public static class DISetup {

        public static void Setup(IServiceCollection services) {

            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
    }
}
