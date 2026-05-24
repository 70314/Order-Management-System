using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OMS.Application.Interfaces;
using OMS.Domain.Interfaces;
using OMS.Infrastructure.Authentication;

namespace OMS.Infrastructure {
    public static class DISetup {

        public static void Setup(IServiceCollection services, IConfiguration? configuration = null) {

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Zitadel authentication (conditional)
            if (configuration != null) {
                var useZitadel = configuration.GetValue<bool>("Authentication:UseZitadelAuth");
                if (useZitadel) {
                    services.Configure<ZitadelOptions>(configuration.GetSection(ZitadelOptions.SectionName));
                    services.AddSingleton<IZitadelTokenValidator, ZitadelTokenValidator>();
                }
            }
        }
    }
}
