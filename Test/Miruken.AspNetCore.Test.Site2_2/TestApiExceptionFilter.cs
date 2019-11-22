namespace Miruken.AspNetCore.Test.Site2_2
{
    using System.Collections.Generic;
    using System.Net;
    using Api;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Validate;

    public class TestApiExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var exception = context.Exception;

            if (exception is ValidationException validationException)
            {
                var errors = new List<string>();
                CollectErrors(validationException.Outcome, errors);

                context.ExceptionHandled = true;
                var response = context.HttpContext.Response;
                response.StatusCode  = 422;
                response.ContentType = "application/json";
                context.Result = new ObjectResult(errors);
            }
            else if (exception is NotFoundException)
            {
                context.ExceptionHandled = true;
                var response = context.HttpContext.Response;
                response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Result = new ObjectResult(exception.Message);
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
