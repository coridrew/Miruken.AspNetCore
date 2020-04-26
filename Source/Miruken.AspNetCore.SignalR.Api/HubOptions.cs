namespace Miruken.AspNetCore.SignalR.Api
{
    using System;
    using Callback;
    using Microsoft.AspNetCore.Http.Connections;
    using Microsoft.AspNetCore.Http.Connections.Client;

    public class HubOptions : Options<HubOptions>
    {
        public HttpTransportType?            HttpTransportType { get; set; }
        public TimeSpan?                     HandshakeTimeout  { get; set; }
        public TimeSpan?                     KeepAliveInterval { get; set; }
        public TimeSpan?                     ServerTimeout     { get; set; }
        public Action<HttpConnectionOptions> HttpOptions       { get; set; }

        public override void MergeInto(HubOptions other)
        {
            if (HttpTransportType.HasValue && !other.HttpTransportType.HasValue)
                other.HttpTransportType = HttpTransportType;

            if (HandshakeTimeout.HasValue && !other.HandshakeTimeout.HasValue)
                other.HandshakeTimeout = HandshakeTimeout;

            if (KeepAliveInterval.HasValue && !other.KeepAliveInterval.HasValue)
                other.KeepAliveInterval = KeepAliveInterval;

            if (ServerTimeout.HasValue && !other.ServerTimeout.HasValue)
                other.ServerTimeout = ServerTimeout;

            if (HttpOptions != null && other.HttpOptions == null)
                other.HttpOptions = HttpOptions;
        }
    }

    public static class HubOptionsExtensions
    {
        public static IHandler HubOptions(
            this IHandler handler, HubOptions hubOptions)
        {
            return hubOptions.Decorate(handler);
        }

        public static IHandler HttpTransportType(
            this IHandler handler, HttpTransportType httpTransportType)
        {
            return new HubOptions { HttpTransportType = httpTransportType }.Decorate(handler);
        }

        public static IHandler HandshakeTimeout(
            this IHandler handler, TimeSpan handshakeTimeout)
        {
            return new HubOptions { HandshakeTimeout = handshakeTimeout }.Decorate(handler);
        }

        public static IHandler KeepAliveInterval(
            this IHandler handler, TimeSpan keepAliveInterval)
        {
            return new HubOptions { KeepAliveInterval = keepAliveInterval }.Decorate(handler);
        }

        public static IHandler ServerTimeout(
            this IHandler handler, TimeSpan serverTimeout)
        {
            return new HubOptions { ServerTimeout = serverTimeout }.Decorate(handler);
        }

        public static IHandler HubHttpOptions(
            this IHandler handler, Action<HttpConnectionOptions> httpOptions)
        {
            return new HubOptions { HttpOptions = httpOptions }.Decorate(handler);
        }
    }
}
