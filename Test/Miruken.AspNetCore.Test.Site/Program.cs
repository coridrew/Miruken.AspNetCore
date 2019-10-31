namespace Miruken.AspNetCore.Test.Site
{
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;

    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .UseIIS()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();
    }
}
