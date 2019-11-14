namespace Miruken.AspNetCore.Test.Site
{
    using System;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Register;
    using Swagger;
    using Swashbuckle.AspNetCore.Swagger;
    using Tests;
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
            services.AddMvc(config =>
            {
                config.Filters.Add(typeof(TestApiExceptionFilter));
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Version     = "v1",
                    Title       = "Test Api",
                    Description = "Swagger Integration with Miruken"
                });
                c.AddMiruken();
            });

            return services.AddMiruken(configure => configure
                .PublicSources(sources => sources.FromAssemblyOf<PlayerHandler>())
                .WithAspNet(options => options.AddControllers())
                .WithValidation()
            ).Build();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc()
                .UseSwagger()
                .UseSwaggerUI(c =>
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Test Api"));
        }
    }
}
