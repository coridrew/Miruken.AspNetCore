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

        public event Func<Operation, bool> Operations;


        public void Apply(SwaggerDocument document, DocumentFilterContext context)
        {
            var registry = context.SchemaRegistry;

            var bindings = Handles.Policy.GetMethods();
            AddPaths(document, registry, "process", bindings);

            document.Paths = document.Paths.OrderBy(e => e.Key)
                .ToDictionary(e => e.Key, e => e.Value);

            var messageName = typeof(Message).FullName ?? "Message";
            document.Definitions[messageName].Example = new Message();
        }

        private void AddPaths(SwaggerDocument document, ISchemaRegistry registry,
            string resource, IEnumerable<PolicyMemberBinding> bindings)
        {
            foreach (var path in BuildPaths(resource, registry, bindings))
            {
                if (!document.Paths.ContainsKey(path.Item1))
                    document.Paths.Add(path.Item1, path.Item2);
            }
        }

        private IEnumerable<Tuple<string, PathItem>> BuildPaths(
            string resource, ISchemaRegistry registry,
            IEnumerable<PolicyMemberBinding> bindings)
        {
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
                        }
                    }
                };

                if (Operations != null && Operations.GetInvocationList()
                        .Cast<Func<Operation, bool>>()
                        .Any(op => !op(operation)))
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
}
