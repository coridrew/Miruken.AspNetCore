namespace Miruken.AspNetCore
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

#if NETSTANDARD2_0
    using System.Buffers;
    using Microsoft.Extensions.ObjectPool;
    using Http;
#endif

    public class HttpRouteBodyModelBinder : IModelBinder
    {
#if NETSTANDARD2_0
        private readonly JsonInputFormatter _input;
#else
        private readonly SystemTextJsonInputFormatter _input;
#endif
        private readonly Func<Stream, Encoding, TextReader> _readerFactory;

#if NETSTANDARD2_0
        public HttpRouteBodyModelBinder(
            IHttpRequestStreamReaderFactory readerFactory,
            ILoggerFactory loggerFactory,
            IOptions<MvcOptions> options, IOptions<MvcJsonOptions> jsonOptions,
            ArrayPool<char> charPool, ObjectPoolProvider objectPoolProvider)
        {

            _input = new JsonInputFormatter(
                loggerFactory.CreateLogger(typeof(HttpRouteBodyModelBinder)),
                HttpFormatters.Route.SerializerSettings, charPool,
                objectPoolProvider, options.Value, jsonOptions.Value);

            _readerFactory = readerFactory.CreateReader;         
        }
#else
        public HttpRouteBodyModelBinder(
            IHttpRequestStreamReaderFactory readerFactory,
            ILoggerFactory loggerFactory)
        {
            _input = new SystemTextJsonInputFormatter(new JsonOptions(),
                loggerFactory.CreateLogger<SystemTextJsonInputFormatter>());

            _readerFactory = readerFactory.CreateReader;
        }
#endif
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var httpContext     = bindingContext.HttpContext;
            var modelBindingKey = bindingContext.IsTopLevelObject
                ? bindingContext.BinderModelName ?? string.Empty
                : bindingContext.ModelName;

            var formatterContext = new InputFormatterContext(
                httpContext, modelBindingKey,
                bindingContext.ModelState,
                bindingContext.ModelMetadata,
                _readerFactory, true);

            try
            {
                var result = await _input.ReadAsync(formatterContext);

                if (result.HasError) return;

                if (result.IsModelSet)
                {
                    var model = result.Model;
                    bindingContext.Result = ModelBindingResult.Success(model);
                }
                else
                {
                    var message = bindingContext
                        .ModelMetadata
                        .ModelBindingMessageProvider
                        .MissingRequestBodyRequiredValueAccessor();
                    bindingContext.ModelState.AddModelError(modelBindingKey, message);
                }
            }
            catch (Exception exception) 
                when (exception is InputFormatterException || ShouldHandleException(_input))
            {
                bindingContext.ModelState.AddModelError(modelBindingKey,
                    exception, bindingContext.ModelMetadata);
            }
        }

        private static bool ShouldHandleException(IInputFormatter formatter)
        {
            var policy = (formatter as IInputFormatterExceptionPolicy)?.ExceptionPolicy ??
                         InputFormatterExceptionPolicy.MalformedInputExceptions;
            return policy == InputFormatterExceptionPolicy.AllExceptions;
        }
    }
}
