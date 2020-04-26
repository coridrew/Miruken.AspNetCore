namespace Miruken.AspNetCore.SignalR.Api
{
    using System;
    using System.Threading.Tasks;
    using Callback;
    using Miruken.Api;

    public static class HandlerSignalRExtensions
    {
        public static Task<HubConnectionInfo> ConnectHub(
            this IHandler handler, string url) => handler.Send(new HubConnect(new Uri(url)));

        public static Task<HubConnectionInfo> ConnectHub(
            this IHandler handler, Uri url) => handler.Send(new HubConnect(url));

        public static Task DisconnectHub(this IHandler handler, string url) =>
            handler.DisconnectHub(new Uri(url));

        public static Task DisconnectHub(this IHandler handler, Uri url) =>
            handler.Send(new HubDisconnect(url));
    }
}
