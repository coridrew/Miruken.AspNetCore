namespace Miruken.AspNetCore.SignalR.Test.Site
{
    using Api;
    using Callback;
    using Microsoft.Extensions.Logging;
    using Tests;

    public class PlayerListener : Handler
    {
        private readonly ILogger _logger;

        public PlayerListener(ILogger logger)
        {
            _logger = logger;
        }

        [Handles]
        public void Added(
            PlayerResponse    response,
            HubConnectionInfo connectionInfo,
            IHandler          composer)
        {
            var player = response.Player;
            _logger.LogInformation($"Player {player.Id} created ({player.Name})");

            if (player.Name == "Pele")
                composer.DisconnectHub(connectionInfo.Url);
        }

        [Handles]
        public void Reconnecting(HubReconnecting reconnecting)
        {
            var connectionInfo = reconnecting.ConnectionInfo;
            _logger.LogInformation($"Client {connectionInfo.Id} reconnecting to {connectionInfo.Url}");
        }

        [Handles]
        public void Reconnected(HubReconnected reconnected)
        {
            var connectionInfo = reconnected.ConnectionInfo;
            _logger.LogInformation($"Client {connectionInfo.Id} reconnected to {connectionInfo.Url}");
        }

        [Handles]
        public void Closed(HubClosed closed)
        {
            var connectionInfo = closed.ConnectionInfo;
            _logger.LogInformation($"Client disconnected from {connectionInfo.Url}");

        }
    }
}
