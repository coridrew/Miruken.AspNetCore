namespace Miruken.AspNetCore.Swagger
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Swashbuckle.AspNetCore.SwaggerGen;

#if NETSTANDARD2_1
    using Microsoft.OpenApi.Models;
#elif NETSTANDARD2_0
    using Swashbuckle.AspNetCore.Swagger;
#endif

    public static class SwaggerGenExtensions
    {
#if NETSTANDARD2_1
        public static SwaggerGenOptions AddMiruken(this SwaggerGenOptions options,
            Predicate<OpenApiOperation> operationFilter = null)
        {
            options.DocumentFilter<MirukenDocumentFilter>(operationFilter ?? (_ => true));
            return options;
        }
#elif NETSTANDARD2_0
        public static SwaggerGenOptions AddMiruken(this SwaggerGenOptions options,
            Predicate<Operation> operationFilter = null)
        {
            options.CustomSchemaIds(MirukenDocumentFilter.ModelToSchemaId);
            options.DocumentFilter<MirukenDocumentFilter>(operationFilter ?? (_ => true));
            return options;
        }
#endif
    }
}


