namespace Miruken.AspNetCore.SignalR.Api
{
    using System;
    using Microsoft.AspNetCore.Http.Connections;
    using Microsoft.AspNetCore.Http.Connections.Client;
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

        public Uri                           Url               { get; set; }
        public HttpTransportType?            HttpTransportType { get; set; }
        public TimeSpan?                     HandshakeTimeout  { get; set; }
        public TimeSpan?                     KeepAliveInterval { get; set; }
        public TimeSpan?                     ServerTimeout     { get; set; }
        public Action<HttpConnectionOptions> Options           { get; set; }
    }
}
