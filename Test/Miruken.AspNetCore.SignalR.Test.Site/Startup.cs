namespace Miruken.AspNetCore.SignalR.Test.Site
{
    using Api;
    using AspNetCore;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Register;
    using SignalR;
    using Tests;
    using Validate;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging();
            services.AddRazorPages();
            services.AddSignalR()
                .AddHubOptions<MessageHub>(options =>
                {
                    options.EnableDetailedErrors = true;
                });

            services.AddMiruken(configure => configure
                .PublicSources(
                    sources => sources.FromAssemblyOf<Startup>(),
                    sources => sources.FromAssemblyOf<PlayerHandler>(),
                    sources => sources.FromAssemblyOf<HubRouter>())
                .WithAspNet()
                .WithValidation()
            );

            services.AddHostedService<PlayerService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapHub<MessageHub>("/hub/miruken");
            });
        }
    }
}
