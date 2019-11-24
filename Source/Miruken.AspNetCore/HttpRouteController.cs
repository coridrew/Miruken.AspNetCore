namespace Miruken.AspNetCore
{
    using System;
    using System.Buffers;
    using System.Threading;
    using System.Threading.Tasks;
    using Api;
    using Callback;
    using Context;
    using Http;
    using Http.Format;
    using Map;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Newtonsoft.Json;
    using Validate;

    public class HttpRouteController : ContextualControllerBase
    {
        [Provides, Contextual]
        public HttpRouteController()
        {          
        }

        [HttpPost,
         Route("process/{*rest}", Name = "Process"),
         Route("tag/{client}/{*args}", Name = "ProcessTag")]
        public async Task<IActionResult> Process([HttpRouteBody]Message message)
        {
            var settings = CreateSerializerSettings();

            if (!ModelState.IsValid)
                return CreateInvalidMessageResponse(settings);

            var request = message?.Payload;
            if (request == null)
            {
                return CreateErrorResponse(new ArgumentException(
                        "Request payload is missing"), settings,
                    StatusCodes.Status400BadRequest);
            }

            Context.Store(User ?? Thread.CurrentPrincipal);

            try
            {
                var response = await Context.Send(request);
                return CreateResult(new Message(response), settings);
            }
            catch (Exception exception)
            {
                return CreateErrorResponse(exception, settings);
            }
        }

        [HttpPost, Route("publish/{*rest}", Name = "Publish")]
        public async Task<IActionResult> Publish([FromBody]Message message)
        {
            var settings = CreateSerializerSettings();

            if (!ModelState.IsValid)
                return CreateInvalidMessageResponse(settings);

            var notification = message?.Payload;
            if (notification == null)
            {
                return CreateErrorResponse(new ArgumentException(
                        "Notification payload is missing"), settings,
                    StatusCodes.Status400BadRequest);
            }

            Context.Store(User ?? Thread.CurrentPrincipal);

            try
            {
                await Context.Publish(notification);
                return CreateResult(new Message(), settings);
            }
            catch (Exception exception)
            {
                return CreateErrorResponse(exception, settings);
            }
        }

        private IActionResult CreateErrorResponse(Exception exception,
            JsonSerializerSettings settings, int? code = null)
        {
            var bestEffort = Context.BestEffort();

            if (!code.HasValue)
            {
                code = bestEffort.Map<int>(exception);
                if (code == 0) code = StatusCodes.Status500InternalServerError;
            }

            var error = bestEffort.Map<object>(exception, typeof(Exception))
                     ?? new ExceptionData(exception);

            return CreateResult(new Message(error), settings, code);
        }

        private IActionResult CreateInvalidMessageResponse(
            JsonSerializerSettings settings)
        {
            var outcome = new ValidationOutcome();
            foreach (var property in ModelState)
                foreach (var error in property.Value.Errors)
                {
                    var key = property.Key;
                    if (key.StartsWith("message."))
                        key = key.Substring(8);
                    var message = error.Exception?.Message ?? error.ErrorMessage;
                    outcome.AddError(key, message);
                }

            return CreateErrorResponse(
                new ValidationException(outcome), settings,
                StatusCodes.Status400BadRequest);
        }

        private JsonSerializerSettings CreateSerializerSettings()
        {
            var settings = HttpFormatters.Route.SerializerSettings.Copy();
            settings.Converters.Add(new ExceptionJsonConverter(Context));
            return settings;
        }

        private static IActionResult CreateResult(object value,
            JsonSerializerSettings settings, int? statusCode = null)
        {
#if NETSTANDARD2_0
            return new JsonResult(value, settings) 
            {
                StatusCode = statusCode
            };
#elif NETSTANDARD2_1
            return new ObjectResult(value)
            {
                Formatters = { 
                    new NewtonsoftJsonOutputFormatter(
                        settings, ArrayPool<char>.Shared, new MvcOptions())
                },
                StatusCode = statusCode
            };
#endif
        }
    }
}
