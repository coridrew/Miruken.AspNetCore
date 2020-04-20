namespace Miruken.AspNetCore.SignalR
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Api;
    using Callback;
    using Microsoft.AspNetCore.SignalR;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NewtonsoftJsonSerializer = Newtonsoft.Json.JsonSerializer;

    public class MessageHub : Hub
    {
        private readonly IHandler _handler;

        private static readonly NewtonsoftJsonSerializer JsonSerializer =
            NewtonsoftJsonSerializer.Create(Http.HttpFormatters.Route.SerializerSettings);

        public class Message
        {
            public object Payload { get; set; }
        }

        public MessageHub(IHandler handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public async Task<Message> Process(Message message)
        {
            var context = Context;
            var (request, system) = ExtractRequest(message?.Payload);

            var response = await _handler
                .With(context)
                .With(context.User ?? Thread.CurrentPrincipal)
                .Send(request);

            return CreateResult(response, system);
        }

        public async Task Publish(Message message)
        {
            var context = Context;
            var (notification, _) = ExtractRequest(message?.Payload);

            await _handler
                .With(context)
                .With(context.User ?? Thread.CurrentPrincipal)
                .Publish(notification);

            await Clients.Others.SendAsync("Publish", message);
        }

        private static (object, bool) ExtractRequest(object payload)
        {
            if (payload == null)
                throw new ArgumentException("Request payload is missing");

            return payload switch
            {
                JsonElement json => /* System.Text.Json */
                    (JsonConvert.DeserializeObject(json.GetRawText(),
                        Http.HttpFormatters.Route.SerializerSettings), true),
                JObject json =>     /* Newtonsoft.Json */
                    (JsonSerializer.Deserialize(new JTokenReader(json)), false),
                _ => throw new InvalidOperationException(
                        $"Unrecognized payload type '{payload.GetType().FullName}'")
            };
        }

        private static Message CreateResult(object response, bool system)
        {
            var result = new Message();

            if (response == null) return result;

            if (!system)
            {
                result.Payload = response;
                return result;
            }

            var wrapper = JsonConvert.SerializeObject(
                new Message { Payload = response },
                Http.HttpFormatters.Route.SerializerSettings);

            result.Payload = JsonDocument.Parse(wrapper)
                .RootElement.GetProperty("payload");

            return result;
        }
    }
}
