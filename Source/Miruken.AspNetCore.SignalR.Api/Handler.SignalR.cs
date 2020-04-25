namespace Miruken.AspNetCore.SignalR.Api
{
    using System;
    using System.Threading.Tasks;
    using Callback;
    using Miruken.Api;

    public static class HandlerSignalRExtensions
    {
        public static Task<HubConnectionInfo> ConnectHub(
            this IHandler handler, string url, Action<HubConnect> options = null)
        {
            return handler.ConnectHub(new Uri(url), options);
        }

        public static Task<HubConnectionInfo> ConnectHub(
            this IHandler handler, Uri url, Action<HubConnect> options = null)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            var connect = new HubConnect(url);
            options?.Invoke(connect);
            return handler.Send(connect);
        }

        public static Task DisconnectHub(this IHandler handler, string url) =>
            handler.DisconnectHub(new Uri(url));

        public static Task DisconnectHub(this IHandler handler, Uri url) =>
            handler.Send(new HubDisconnect(url));
    }
}
