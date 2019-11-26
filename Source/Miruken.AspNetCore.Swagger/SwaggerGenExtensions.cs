namespace Miruken.AspNetCore.Swagger
{
    using Microsoft.Extensions.DependencyInjection;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public static class SwaggerGenExtensions
    {
        public static SwaggerGenOptions AddMiruken(this SwaggerGenOptions options)
        {
#if NETSTANDARD2_0
            options.CustomSchemaIds(MirukenDocumentFilter.ModelToSchemaId);
#endif
            options.DocumentFilter<MirukenDocumentFilter>();
            return options;
        }
    }
}


