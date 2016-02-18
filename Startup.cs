using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(TransactionManagement.Startup))]

namespace TransactionManagement
{
    using System.Net.Http.Formatting;
    using System.Web.Http;
    using LightInject;

    public class Startup
    {
        public Startup()
        {
            Container = new ServiceContainer();            
        }

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            Configure(Container);
            ConfigureMediaFormatter(config);
            ConfigureHttpRoutes(config);
            Container.RegisterApiControllers();
            Container.EnableWebApi(config);
            app.UseWebApi(config);
        }

        private static void ConfigureMediaFormatter(HttpConfiguration configuration)
        {
            configuration.Formatters.Clear();
            configuration.Formatters.Add(new JsonMediaTypeFormatter());
        }

        private static void ConfigureHttpRoutes(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "API Default",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });
        }

        public virtual void Configure(IServiceContainer serviceContainer)
        {
            serviceContainer.RegisterFrom<CompositionRoot>();        
        }

        public IServiceContainer Container { get; }         
    }
}
