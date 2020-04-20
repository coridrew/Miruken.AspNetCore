// ReSharper disable CheckNamespace
namespace Miruken.AspNetCore
{
    using Microsoft.AspNetCore.SignalR;
    using Scrutor;

    public partial class AspNetOptions
    {
        public AspNetOptions AddHubs()
        {
            _registration.Select((selector, publicOnly) =>
                selector.AddClasses(x => x.AssignableTo<Hub>(), publicOnly)
                    .UsingRegistrationStrategy(RegistrationStrategy.Skip)
                    .AsSelf().WithScopedLifetime());
            return this;
        }
    }
}
