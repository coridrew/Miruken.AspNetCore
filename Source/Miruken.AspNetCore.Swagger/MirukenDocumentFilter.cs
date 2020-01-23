#if NETSTANDARD2_0
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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Swashbuckle.AspNetCore.Swagger;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class MirukenDocumentFilter : IDocumentFilter
    {
        private readonly Predicate<Operation> _operationFilter;
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

        public MirukenDocumentFilter(Predicate<Operation> operationFilter)
        {
            _operationFilter = operationFilter;
            _examples        = CreateExamplesGenerator();
        }

        public static bool NoInfrastructure(Operation operation)
        {
            return operation == null || !operation.Tags.Any(tag =>
                       InfrastructureTags.Any(t => tag?.StartsWith(t) == true));
        }

        private static readonly string[] InfrastructureTags = { "Miruken", "HttpRoute" };

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

        public void Apply(SwaggerDocument document, DocumentFilterContext context)
        {
            var registry = context.SchemaRegistry;

            if (_operationFilter != null)
            {
                var pathsToRemove = document.Paths.Where(pathItem =>
                    {
                        var path = pathItem.Value;
                        if (!_operationFilter(path.Get)) return true;
                        if (!_operationFilter(path.Delete)) return true;
                        if (!_operationFilter(path.Head)) return true;
                        if (!_operationFilter(path.Options)) return true;
                        if (!_operationFilter(path.Patch)) return true;
                        if (!_operationFilter(path.Post)) return true;
                        return !_operationFilter(path.Put);
                    })
                    .ToList();

                foreach (var item in pathsToRemove)
                    document.Paths.Remove(item.Key);
            }

            var bindings = Handles.Policy.GetMethods();
            AddPaths(document, registry, "process", bindings);

            var messageName = typeof(Message).FullName ?? "Message";
            document.Definitions[messageName].Example = new Message();
        }

        private void AddPaths(SwaggerDocument document, ISchemaRegistry registry,
            string resource, IEnumerable<PolicyMemberBinding> bindings)
        {
            foreach (var (key, path) in BuildPaths(resource, registry, bindings))
            {
                if (!document.Paths.ContainsKey(key))
                    document.Paths.Add(key, path);
            }
        }

        private IEnumerable<Tuple<string, PathItem>> BuildPaths(
            string resource, ISchemaRegistry registry,
            IEnumerable<PolicyMemberBinding> bindings)
        {
            var validationErrorsSchema = registry.GetOrRegister(typeof(ValidationErrors[]));
            validationErrorsSchema.Example = new[]
            {
                new ValidationErrors
                {
                    PropertyName = "SomeProperty",
                    Errors       = new [] { "'Some Property' is required" },
                    Nested       = new []
                    {
                        new ValidationErrors
                        {
                            PropertyName = "NestedProperty",
                            Errors       = new [] { "'Nested Property' not in range"}
                        }
                    }
                }
            };

            return bindings.Select(x =>
            {
                var requestType = x.Key as Type;
                if (requestType == null || requestType.IsAbstract ||
                    requestType.ContainsGenericParameters)
                    return null;
                var responseType   = x.Dispatcher.LogicalReturnType;
                var handler        = x.Dispatcher.Owner.HandlerType;
                var assembly       = requestType.Assembly.GetName();
                var tag            = $"{assembly.Name} - {assembly.Version}";
                var requestSchema  = GetMessageSchema(registry, requestType);
                var responseSchema = GetMessageSchema(registry, responseType);
                var requestPath    = HttpOptionsExtensions.GetRequestPath(requestType);

                var requestSummary = GetReferencedSchema(registry,
                    registry.GetOrRegister(requestType))?.Description;

                var handlerAssembly = handler.Assembly.GetName();
                var handlerNotes    = $"Handled by {handler.FullName} in {handlerAssembly.Name} - {handlerAssembly.Version}";

                var operation = new Operation
                {
                    Summary     = requestSummary,
                    OperationId = requestType.FullName,
                    Description = handlerNotes,
                    Tags        = new List<string> {tag},
                    Consumes    = JsonFormats,
                    Produces    = JsonFormats,
                    Parameters  = new List<IParameter>
                    {
                        new BodyParameter
                        {
                            In          = "body",
                            Name        = "message",
                            Description = "request to process",
                            Schema      = requestSchema,
                            Required    = true
                        }
                    },
                    Responses = new Dictionary<string, Response>
                    {
                        {
                            "200", new Response
                            {
                                Description = "OK",
                                Schema      = responseSchema
                            }
                        },
                        {
                            "422", new Response
                            {
                                Description = "Validation Errors",
                                Schema      = validationErrorsSchema
                            }
                        }
                    }
                };

                if (_operationFilter.Invoke(operation) == false)
                    return null;

                return Tuple.Create($"/{resource}/{requestPath}", new PathItem
                {
                    Post = operation
                });
            }).Where(p => p != null);
        }

        private static Schema GetReferencedSchema(ISchemaRegistry registry, Schema reference)
        {
            var parts = reference.Ref.Split('/');
            var name = parts.Last();
            return registry.Definitions[name];
        }

        private Schema GetMessageSchema(ISchemaRegistry registry, Type message)
        {
            if (message == null || message == typeof(void) || message == typeof(object))
                return registry.GetOrRegister(typeof(Message));
            var schema = registry.GetOrRegister(typeof(Message<>).MakeGenericType(message));
            var definition = GetReferencedSchema(registry, schema);
            definition.Example = CreateExampleMessage(message);
            return schema;
        }

        private object CreateExampleMessage(Type message)
        {
            try
            {
                var creator    = CreateExampleMethod.MakeGenericMethod(message);
                var example    = creator.Invoke(null, new object[] { _examples });
                var jsonString = JsonConvert.SerializeObject(example, SerializerSettings);
                return JsonConvert.DeserializeObject(jsonString);
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

    public class ValidationErrors
    {
        public string             PropertyName { get; set; }
        public string[]           Errors       { get; set; }
        public ValidationErrors[] Nested       { get; set; }
    }
}
#endif

