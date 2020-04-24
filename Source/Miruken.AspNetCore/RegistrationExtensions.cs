namespace Miruken.AspNetCore
{
    using System;
    using Http;
    using Register;

    public static class RegistrationExtensions
    {
        public static Registration WithAspNet(this Registration registration,
            Action<AspNetOptions> configure = null)
        {
            if (!registration.CanRegister(typeof(RegistrationExtensions)))
                return registration;

            registration.WithHttp()
                .Sources(sources => sources.FromAssemblyOf<HttpRouteController>());

            if (configure != null)
            {
                return registration.Services(services =>
                {
                    var options = new AspNetOptions(services, registration);
                    configure(options);
                });
            }

            return registration;
        }
    }
}
