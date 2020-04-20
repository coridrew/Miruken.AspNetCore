namespace Miruken.AspNetCore.SignalR.Api
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Callback;
    using Infrastructure;
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

            if (routed.Message.GetType().IsClassOf(typeof(IRequest<>)))
            {
                var response = await connection.InvokeAsync<HubMessage>(Process, message);
                return response?.Payload;
            }

            if (command.Many)
                await connection.InvokeAsync(Publish, message);
            else
                await connection.InvokeAsync(Process, message);

            return null;
        }

        [Handles]
        public async Task<HubConnectionInfo> Connect(HubConnect connect, IHandler composer)
        {
            var connection = await GetConnection(connect.Url, composer);
            return GetConnectionInfo(connection, connect.Url);
        }

        [Handles]
        public Task Disconnect(HubDisconnect disconnect)
        {
            return Disconnect(disconnect.Url);
        }

        private async Task<HubConnection> GetConnection(Uri url, IHandler composer)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            if (_connections.TryGetValue(url, out var connection) &&
                connection.State != HubConnectionState.Disconnected)
                return connection;

            if (_connections.TryRemove(url, out connection))
                await connection.DisposeAsync();

            connection = new HubConnectionBuilder()
                .WithUrl(url)
#if NETSTANDARD2_1
                .WithAutomaticReconnect()
#endif
                .AddNewtonsoftJsonProtocol(options =>
                    {
                        options.PayloadSerializerSettings =
                            Http.HttpFormatters.Route.SerializerSettings;
                    })
                .Build();

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
                .Send(message.Payload));

            connection.On<HubMessage>(Publish, message => composer
                .With(GetConnectionInfo(connection, url))
                .Publish(message.Payload));

            await ConnectWithRetryAsync(connection, url);

#if NETSTANDARD2_1
            connection.Reconnecting += async exception =>
            {
                var reconnecting = new HubReconnecting
                {
                    ConnectionInfo = GetConnectionInfo(connection, url),
                    Exception      = exception
                };
                await notify.Send(reconnecting);
            };

            connection.Reconnected += async connectionId =>
            {
                var reconnecting = new HubReconnected
                {
                    ConnectionInfo  = GetConnectionInfo(connection, url),
                    NewConnectionId = connectionId
                };
                await notify.Send(reconnecting);
            };
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

        private Task Disconnect(Uri url)
        {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            return _connections.TryRemove(url, out var connection)
                 ? connection.DisposeAsync() 
                 : Task.CompletedTask;
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
