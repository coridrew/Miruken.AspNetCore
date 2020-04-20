namespace Miruken.AspNetCore
{
    using System.Linq;
    using System.Reflection;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ApplicationParts;
    using Microsoft.AspNetCore.Mvc.Controllers;
    using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
    using Microsoft.AspNetCore.Mvc.ViewComponents;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Register;
    using Scrutor;

    public partial class AspNetOptions
    {
        private readonly IServiceCollection _services;
        private readonly Registration _registration;
        private readonly ApplicationPartManager _parts;

        public AspNetOptions(IServiceCollection services, Registration registration)
        {
            _services     = services;
            _registration = registration;

            _parts = services.LastOrDefault(
                    d => d.ServiceType == typeof(ApplicationPartManager))
                ?.ImplementationInstance as ApplicationPartManager;

            if (_parts != null)
            {
                var assembly = typeof(HttpRouteController).Assembly;
                if (!_parts.ApplicationParts.OfType<AssemblyPart>()
                    .Any(p => p.Assembly == assembly))
                {
                    _parts.ApplicationParts.Add(new AssemblyPart(assembly));
                }
            }
        }

        public AspNetOptions AddControllers()
        {
            if (_parts != null)
            {
                var feature = new ControllerFeature();
                _parts.PopulateFeature(feature);
                feature.Controllers.Add(typeof(HttpRouteController).GetTypeInfo());
                var controllerTypes = feature.Controllers.Select(c => c.AsType());
                _registration.Sources(sources => sources.AddTypes(controllerTypes));
            }

            _registration.Select((selector, publicOnly) =>
                selector.AddClasses(x => x.AssignableTo<ControllerBase>(), publicOnly)
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelf().WithScopedLifetime());

            _services.Replace(ServiceDescriptor
                .Singleton<IControllerActivator, ServiceBasedControllerActivator>());

            return this;
        }

        public AspNetOptions AddViewComponents()
        {
            if (_parts != null)
            {
                var feature = new ViewComponentFeature();
                _parts.PopulateFeature(feature);
                var viewComponentTypes = feature.ViewComponents.Select(v => v.AsType());
                _registration.Sources(sources => sources.AddTypes(viewComponentTypes));
            }

            _registration.Select((selector, publicOnly) =>
                selector.AddClasses(x => x.AssignableTo<ViewComponent>(), publicOnly)
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelf());

            _services.Replace(ServiceDescriptor
                .Singleton<IViewComponentActivator, ServiceBasedViewComponentActivator>());

            return this;
        }

        public AspNetOptions AddPageModels()
        {
            if (_parts != null)
            {
                var pageModelTypes =
                    from   part in _parts.ApplicationParts.OfType<IApplicationPartTypeProvider>()
                    from   type in part.Types
                    where  type.IsSubclassOf(typeof(PageModel))
                    where  !type.IsAbstract && !type.IsGenericTypeDefinition
                    select type;
                _registration.Sources(sources => sources.AddTypes(pageModelTypes));
            }

            _registration.Select((selector, publicOnly) =>
                selector.AddClasses(x => x.AssignableTo<PageModel>(), publicOnly)
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelf());

            _services.Replace(ServiceDescriptor
                .Singleton<IPageModelActivatorProvider, ServiceBasedPageModelActivatorProvider>());

            return this;
        }

        public AspNetOptions AddTagHelpers()
        {
            if (_parts != null)
            {
                var feature = new TagHelperFeature();
                _parts.PopulateFeature(feature);
                var viewComponentTypes = feature.TagHelpers.Select(v => v.AsType());
                _registration.Sources(sources => sources.AddTypes(viewComponentTypes));
            }

            _registration.Select((selector, publicOnly) =>
                selector.AddClasses(x => x.AssignableTo<PageModel>(), publicOnly)
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelf());

            return this;
        }
    }
}
