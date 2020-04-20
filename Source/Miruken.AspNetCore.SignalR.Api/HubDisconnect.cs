namespace Miruken.AspNetCore.SignalR.Api
{
    using System;

    public class HubDisconnect
    {
        public HubDisconnect()
        {
        }

        public HubDisconnect(Uri url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
        }

        public Uri Url { get; set; }
    }
}
