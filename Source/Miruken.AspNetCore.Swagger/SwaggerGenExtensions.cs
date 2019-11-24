#if NETSTANDARD2_0
namespace Miruken.AspNetCore.Swagger
{
    using Microsoft.Extensions.DependencyInjection;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static class SwaggerGenExtensions
    {
        public static SwaggerGenOptions AddMiruken(this SwaggerGenOptions options)
        {
            options.CustomSchemaIds(MirukenDocumentFilter.ModelToSchemaId);
            options.DocumentFilter<MirukenDocumentFilter>();
            return options;
        }
    }
}
#endif

