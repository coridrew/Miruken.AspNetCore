using System;

namespace Miruken.AspNetCore.SignalR.Api
{
    public class HubConnectionInfo
    {
        public HubConnectionInfo()
        {
        }

        public HubConnectionInfo(Uri url)
        {
            Url = url ?? throw new ArgumentNullException(nameof(url));
        }

        public string Id  { get; set; }
        public Uri    Url { get; set; }
    }
}
