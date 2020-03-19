namespace Miruken.AspNetCore.Tests
{
    using Http;
    using Microsoft.AspNetCore.Mvc;
        
    [Route("no-mapping")]
    public class NoMappingController : ControllerBase
    {
        public class SomeError { }

        [HttpPost, Route("process")]
        public IActionResult Error([HttpRouteBody]Message _)
        {
            return new ContentResult
            {
                Content = @"{
                       'payload': {
                           '$type': 'Miruken.AspNetCore.Tests.NoMappingController+SomeError, Miruken.AspNetCore.Tests',
                        }
                    }",
                ContentType = "application/json",
                StatusCode  = 500
            };
        }
    }

    [Route("no-type-good")]
    public class NoTypeGoodController : ControllerBase
    {
        [HttpPost, Route("process")]
        public IActionResult Error([HttpRouteBody]Message _)
        {
            return new ContentResult
            {
                Content = @"{
                       'payload': {
                           '$type': 'Miruken.AspNetCore.Tests.SomeError, Miruken.AspNetCore.Tests',
                        }
                    }",
                ContentType = "application/json",
                StatusCode  = 200
            };
        }
    }

    [Route("no-type-bad")]
    public class NoTypeBadController : ControllerBase
    {
        [HttpPost, Route("process")]
        public IActionResult Error([HttpRouteBody]Message _)
        {
            return new ContentResult
            {
                Content = @"{
                       'payload': {
                           '$type': 'Miruken.AspNetCore.Tests.SomeError, Miruken.AspNetCore.Tests',
                        }
                    }",
                ContentType = "application/json",
                StatusCode  = 500
            };
        }
    }
}
