namespace Miruken.AspNetCore.SignalR.Test.Site
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Api;
    using Callback;
    using Microsoft.Extensions.Hosting;
    using Miruken.Api;
    using Miruken.Api.Route;
    using Tests;

    public class PlayerService : BackgroundService
    {
        private readonly IHandler _handler;

        public PlayerService(IHandler handler)
        {
            _handler = handler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _handler.ConnectHub("https://localhost:44305/hub/miruken");

            await Task.Delay(10000, stoppingToken);

            var response = await _handler.Send(new CreatePlayer
            {
                Player = new Player
                {
                    Name   = "Matthew Neuwirt",
                    Person = new Person
                    {
                        DOB = new DateTime(2007, 6, 14)
                    }
                }
            }.RouteTo("hub:https://localhost:44305/hub/miruken"));

            await _handler.Publish(response
                .RouteTo("hub:https://localhost:44305/hub/miruken"));
        }
    }
}
