namespace Miruken.AspNetCore
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Authentication;
    using Api;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Validate;

    public class ApiExceptionFilter : IExceptionFilter
    {
        public virtual void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            switch (exception)
            {
                case ValidationException validationException:
                {
                    var errors = new List<string>();
                    CollectErrors(validationException.Outcome, errors);
                    context.ExceptionHandled = true;
                    var response = context.HttpContext.Response;
                    response.StatusCode  = 422;
                    response.ContentType = "application/json";
                    context.Result       = new ObjectResult(errors);
                    break;
                }
                case NotFoundException _:
                {
                    context.ExceptionHandled = true;
                    var response        = context.HttpContext.Response;
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Result      = new ObjectResult(exception.Message);
                    break;
                }
                case ArgumentException _:
                {
                    context.ExceptionHandled = true;
                    var response        = context.HttpContext.Response;
                    response.StatusCode = (int) HttpStatusCode.BadRequest;
                    context.Result      = new ObjectResult(exception.Message);
                    break;
                }
                case NotSupportedException _:
                {
                    context.ExceptionHandled = true;
                    var response        = context.HttpContext.Response;
                    response.StatusCode = (int)HttpStatusCode.NotImplemented;
                    context.Result      = new ObjectResult(exception.Message);
                    break;
                }
                case AuthenticationException _:
                {
                    context.ExceptionHandled = true;
                    var response        = context.HttpContext.Response;
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Result      = new ObjectResult(exception.Message);
                    break;
                }
                case UnauthorizedAccessException _:
                {
                    context.ExceptionHandled = true;
                    var response        = context.HttpContext.Response;
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    context.Result      = new ObjectResult(exception.Message);
                    break;
                }
            }
        }

        private static void CollectErrors(ValidationOutcome outcome, ICollection<string> errors)
        {
            foreach (var culprit in outcome.Culprits)
            {
                foreach (var error in outcome.GetErrors(culprit))
                {
                    if (error is ValidationOutcome child)
                        CollectErrors(child, errors);
                    else
                        errors.Add(error.ToString());
                }
            }
        }
    }
}
