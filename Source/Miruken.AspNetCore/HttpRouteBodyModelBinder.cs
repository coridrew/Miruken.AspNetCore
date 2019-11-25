namespace Miruken.AspNetCore
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using Microsoft.AspNetCore.Mvc.Infrastructure;
    using Microsoft.AspNetCore.Mvc.ModelBinding;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.ObjectPool;

    public class HttpRouteBodyModelBinder : IModelBinder
    {
        private readonly InputFormatter _input;
        private readonly Func<Stream, Encoding, TextReader> _readerFactory;

        public HttpRouteBodyModelBinder(
            IHttpRequestStreamReaderFactory readerFactory,
            ILoggerFactory loggerFactory, ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider)
        {
#if NETSTANDARD2_0
            _input = new JsonInputFormatter(
                loggerFactory.CreateLogger(typeof(HttpRouteBodyModelBinder)),
                HttpFormatters.Route.SerializerSettings, charPool,
                objectPoolProvider, new MvcOptions(), new MvcJsonOptions());
#elif NETCOREAPP3_0
            _input = new NewtonsoftJsonInputFormatter(
                loggerFactory.CreateLogger(typeof(HttpRouteBodyModelBinder)),
                HttpFormatters.Route.SerializerSettings, charPool,
                objectPoolProvider, new MvcOptions(), new MvcNewtonsoftJsonOptions());
#endif
            _readerFactory = readerFactory.CreateReader;         
        }

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
