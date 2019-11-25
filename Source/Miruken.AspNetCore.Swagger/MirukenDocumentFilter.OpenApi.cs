#if NETSTANDARD2_1
namespace Miruken.AspNetCore.Swagger
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AutoFixture;
    using AutoFixture.Kernel;
    using Callback;
    using Callback.Policy.Bindings;
    using Http;
    using Http.Format;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Swashbuckle.AspNetCore.SwaggerGen;
    using OperationType = Microsoft.OpenApi.Models.OperationType;

    public class MirukenDocumentFilter : IDocumentFilter
    {
        private readonly Fixture _examples;

        private static readonly MethodInfo CreateExampleMethod =
            typeof(MirukenDocumentFilter).GetMethod(nameof(CreateExample),
         BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly JsonSerializerSettings SerializerSettings
            = new JsonSerializerSettings
            {
                NullValueHandling              = NullValueHandling.Ignore,
                ContractResolver               = new CamelCasePropertyNamesContractResolver(),
                TypeNameHandling               = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Converters                     = { EitherJsonConverter.Instance }
            };

        private static readonly string[] JsonFormats = { "application/json" };

        public MirukenDocumentFilter()
        {
            _examples = CreateExamplesGenerator();
        }

        public static string ModelToSchemaId(Type type)
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(Message<>))
            {
                var message = type.GetGenericArguments()[0];
                return $"{typeof(Message).FullName}<{message.FullName}>";
            }
            return type.FullName;
        }

        public event Func<OpenApiOperation, bool> Operations;

        public void Apply(OpenApiDocument document, DocumentFilterContext context)
        {
            var bindings = Handles.Policy.GetMethods();
            AddPaths(document, context, "process", bindings);
        }

        private void AddPaths(OpenApiDocument document, DocumentFilterContext context,
            string resource, IEnumerable<PolicyMemberBinding> bindings)
        {
            foreach (var path in BuildPaths(resource, context, bindings))
            {
                if (!document.Paths.ContainsKey(path.Item1))
                    document.Paths.Add(path.Item1, path.Item2);
            }
        }

        private IEnumerable<Tuple<string, OpenApiPathItem>> BuildPaths(
            string resource, DocumentFilterContext context,
            IEnumerable<PolicyMemberBinding> bindings)
        {
            return bindings.Select(x =>
            {
                var requestType = x.Key as Type;
                if (requestType == null || requestType.IsAbstract ||
                    requestType.ContainsGenericParameters)
                    return null;

                var responseType    = x.Dispatcher.LogicalReturnType;
                var handler         = x.Dispatcher.Owner.HandlerType;
                var assembly        = requestType.Assembly.GetName();
                var tag             = $"{assembly.Name} - {assembly.Version}";
                var requestSchema   = GetMessageSchema(requestType, context);
                var responseSchema  = GetMessageSchema(responseType, context);
                var requestPath     = HttpOptionsExtensions.GetRequestPath(requestType);
                var handlerAssembly = handler.Assembly.GetName();
                var handlerNotes    = $"Handled by {handler.FullName} in {handlerAssembly.Name} - {handlerAssembly.Version}";

                var operation = new OpenApiOperation
                {
                    Summary     = requestSchema.Description,
                    OperationId = requestType.FullName,
                    Description = handlerNotes,
                    Tags        = new List<OpenApiTag> { new OpenApiTag { Name = tag } },
                    RequestBody = new OpenApiRequestBody
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.Schema,
                            Id   = requestSchema.Reference.Id
                        },
                        Description = "request to process",
                        Content     = JsonFormats.Select(f =>
                            new { Format = f, Media = new OpenApiMediaType
                            {
                                Schema = requestSchema
                            } }).ToDictionary(f => f.Format, f => f.Media),
                        Required    = true
                    },
                    Responses =
                    {
                        {
                            "200", new OpenApiResponse
                            {
                                Description = "OK",
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.Schema,
                                    Id   = responseSchema.Reference.Id
                                },
                                Content     = JsonFormats.Select(f =>
                                    new { Format = f, Media = new OpenApiMediaType
                                    {
                                        Schema = responseSchema
                                    } }).ToDictionary(f => f.Format, f => f.Media)
                            }
                        }
                    }
                };

                if (Operations != null && Operations.GetInvocationList()
                        .Cast<Func<OpenApiOperation, bool>>()
                        .Any(op => !op(operation)))
                    return null;

                return Tuple.Create($"/{resource}/{requestPath}", new OpenApiPathItem
                {
                    Operations =
                    {
                        { OperationType.Post, operation }
                    }
                });
            }).Where(p => p != null);
        }

        private OpenApiSchema GetMessageSchema(Type message, DocumentFilterContext context)
        {
            var repository = context.SchemaRepository;
            var generator  = context.SchemaGenerator;

            if (message == null || message == typeof(void) || message == typeof(object))
            {
                return repository.GetOrAdd(
                    typeof(Message),
                    ModelToSchemaId(typeof(Message)), () =>
                    {
                        var s = generator.GenerateSchema(typeof(Message), repository);
                        var jsonString = JsonConvert.SerializeObject(new Message(), SerializerSettings);
                        s.Example = new OpenApiString(jsonString);
                        return s;
                    });
            }

            var genericMessage = typeof(Message<>).MakeGenericType(message);
            return repository.GetOrAdd(genericMessage, ModelToSchemaId(genericMessage), () =>
            {
                var s = generator.GenerateSchema(genericMessage, repository);
                s.Example = CreateExampleMessage(message);
                return s;
            });
        }

        private IOpenApiAny CreateExampleMessage(Type message)
        {
            try
            {
                var creator    = CreateExampleMethod.MakeGenericMethod(message);
                var example    = creator.Invoke(null, new object[] { _examples });
                var jsonString = JsonConvert.SerializeObject(example, SerializerSettings);
                return new OpenApiString(jsonString);
            }
            catch
            {
                return null;
            }
        }

        private static Message<T> CreateExample<T>(ISpecimenBuilder builder)
        {
            return new Message<T> { Payload = builder.Create<T>() };
        }

        private static Fixture CreateExamplesGenerator()
        {
            var generator     = new Fixture { RepeatCount = 1 };
            var customization = new SupportMutableValueTypesCustomization();
            customization.Customize(generator);
            return generator;
        }
    }

    public class Message<T>
    {
        [JsonProperty(TypeNameHandling = TypeNameHandling.All)]
        public T Payload { get; set; }
    }
}
#endif

