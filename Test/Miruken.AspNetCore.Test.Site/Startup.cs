namespace Miruken.AspNetCore.Test.Site
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Register;
    using Validate;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            return services.AddMiruken(configure => configure
                .PublicSources(sources => sources.FromAssemblyOf<Startup>())
                .WithAspNet(options => options.AddControllers())
                .WithValidation()
            );
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc();
        }
    }
}
