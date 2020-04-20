namespace Miruken.AspNetCore.SignalR.Api
{
    using System;
    using Miruken.Api;

    public class HubConnect : IRequest<HubConnectionInfo>
    {
        public HubConnect()
        {
        }

        public HubConnect(Uri url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
        }

        public Uri Url { get; set; }
    }
}
