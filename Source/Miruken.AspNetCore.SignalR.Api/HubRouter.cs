namespace Miruken.AspNetCore.SignalR.Api
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Callback;
    using Microsoft.AspNetCore.SignalR.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Miruken.Api;
    using Miruken.Api.Route;

    [Routes("hub")]
    public class HubRouter : Handler, IDisposable
    {
        private readonly ConcurrentDictionary<Uri, HubConnection>
            _connections = new ConcurrentDictionary<Uri, HubConnection>();

        private const string Process = "Process";
        private const string Publish = "Publish";

        [Handles]
        public async Task<object> Route(Routed routed, Command command, IHandler composer)
        {
            Uri url;
            try
            {
                var uri = new Uri(routed.Route);
                url = new Uri(uri.PathAndQuery);
            }
            catch (UriFormatException)
            {
                return null;
            }

            var connection = await GetConnection(url, composer);

            var message = new HubMessage(routed.Message);

            if (command.Many)
                await connection.InvokeAsync(Publish, message);
            else
            {
                var response = await connection.InvokeAsync<HubMessage>(Process, message);
                return response?.Payload;
            }

            return null;
        }

        [Handles]
        public async Task<HubConnectionInfo> Connect(HubConnect connect, IHandler composer)
        {
            var connection = await GetConnection(connect.Url, composer, connect);
            return GetConnectionInfo(connection, connect.Url);
        }

        [Handles]
        public Task Disconnect(HubDisconnect disconnect)
        {
            return Disconnect(disconnect.Url, true);
        }

        private async Task<HubConnection> GetConnection(
            Uri        url,
            IHandler   composer,
            HubConnect connect = null)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            if (_connections.TryGetValue(url, out var connection) &&
                connection.State != HubConnectionState.Disconnected)
            {
                if (connect != null)
                {
                    throw new InvalidOperationException(
                        $"A connect to the Hub @ {url} already exists.");
                }
                return connection;
            }

            if (_connections.TryRemove(url, out connection))
                await connection.DisposeAsync();

            var options = new HubOptions();
            composer.Handle(options, true);

            var httpOptions = options.HttpOptions;
            var transport   = options.HttpTransportType;

            IHubConnectionBuilder connectionBuilder = new HubConnectionBuilder();

            if (httpOptions != null)
            {
                connectionBuilder = transport != null
                    ? connectionBuilder.WithUrl(url, transport.Value, httpOptions) 
                    : connectionBuilder.WithUrl(url, httpOptions);
            } 
            else if (transport != null)
                connectionBuilder = connectionBuilder.WithUrl(url, transport.Value);
            else
                connectionBuilder = connectionBuilder.WithUrl(url);

#if NETSTANDARD2_1
            connectionBuilder = connectionBuilder.WithAutomaticReconnect();
#endif
               
            connectionBuilder = connectionBuilder.AddNewtonsoftJsonProtocol(json =>
                {
                    json.PayloadSerializerSettings =
                        Http.HttpFormatters.Route.SerializerSettings;
                });

            connection = connectionBuilder.Build();

            if (options.HandshakeTimeout != null)
                connection.HandshakeTimeout = options.HandshakeTimeout.Value;

            if (options.KeepAliveInterval != null)
                connection.KeepAliveInterval = options.KeepAliveInterval.Value;

            if (options.ServerTimeout != null)
                connection.ServerTimeout = options.ServerTimeout.Value;

            var notify = composer.Notify();

            connection.Closed += async exception =>
            {
                var closed = new HubClosed
                {
                    ConnectionInfo = GetConnectionInfo(connection, url),
                    Exception      = exception
                };
                await notify.Send(closed);

                if (exception != null)
                    await Disconnect(url);
                else
                    await ConnectWithRetryAsync(connection, url);
            };

            connection.On<HubMessage>(Process, message => composer
                .With(GetConnectionInfo(connection, url))
                .With(connection)
                .Send(message.Payload));

            connection.On<HubMessage>(Publish, message => composer
                .With(GetConnectionInfo(connection, url))
                .With(connection)
                .Publish(message.Payload));

            await ConnectWithRetryAsync(connection, url);

#if NETSTANDARD2_1
            connection.Reconnecting += exception => notify.Send(
                new HubReconnecting
                {
                    ConnectionInfo = GetConnectionInfo(connection, url),
                    Exception      = exception
                });


            connection.Reconnected += connectionId => notify.Send(
                new HubReconnected
                {
                    ConnectionInfo  = GetConnectionInfo(connection, url),
                    NewConnectionId = connectionId
                });
#endif

            _connections[url] = connection;
            return connection;
        }

        public static async Task ConnectWithRetryAsync(HubConnection connection, Uri url)
        {
            var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

            while (true)
            {
                try
                {
                    await connection.StartAsync(timeout);
                    return;
                }
                catch when (timeout.IsCancellationRequested)
                {
                    throw new TimeoutException($"Unable to connect to the Hub at {url}");
                }
                catch
                {
                    await Task.Delay(5000, timeout);
                }
            }
        }

        private async Task Disconnect(Uri url, bool stop = false)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));



            if (_connections.TryRemove(url, out var connection))
            {
                if (stop)
                {
                    try
                    {
                        await connection.StopAsync();
                    }
                    catch
                    {
                        // ignore
                    }
                }

                await connection.DisposeAsync();
            }
        }

        private static HubConnectionInfo GetConnectionInfo(HubConnection connection, Uri url)
        {
            return new HubConnectionInfo(url)
#if NETSTANDARD2_1
                {
                    Id = connection.ConnectionId
                }
#endif
                ;
        }

        public void Dispose()
        {
            foreach (var connection in _connections.Values)
            {
                try
                {
                    connection.DisposeAsync().Wait();
                }
                catch
                {
                    // Ignore
                }
            }
        }
    }
}
