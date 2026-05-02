using Microsoft.Extensions.DependencyInjection;
using OMS.Application.Interfaces;
using OMS.Application.Services;

namespace OMS.Application {
    public static class DISetup {

        public static void Setup(IServiceCollection services) {

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICustomerService, CustomerService>();
            services.AddScoped<IProductService, ProductService>();
        }
    }
}
