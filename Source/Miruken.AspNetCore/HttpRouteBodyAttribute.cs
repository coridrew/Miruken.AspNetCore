
namespace Miruken.AspNetCore
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding;

    public sealed class HttpRouteBodyAttribute : ModelBinderAttribute
    {
        public HttpRouteBodyAttribute() : base(typeof(HttpRouteModelBinder))
        {
            BindingSource = BindingSource.Body;
        }
    }
}
