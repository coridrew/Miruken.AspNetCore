namespace Miruken.AspNetCore.Test.Site3_1
{
    using System.Threading.Tasks;
    using Api;
    using AspNetCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Tests;

    [ApiController,
     Route("[controller]")]
    public class PlayerController : ContextualControllerBase
    {
        private readonly ILogger _logger;

        public PlayerController(ILogger logger)
        {
            _logger = logger;
        }

        [HttpGet, Route("{id}")]
        public async Task<Player> GetPlayer(int id)
        {
            _logger.LogInformation("Getting player {0}", id);
            return (await Context.Send(new GetPlayer {PlayerId = id})).Player;
        }

        [HttpPost, Route("")]
        public async Task<Player> CreatePlayer(Player player)
        {
            _logger.LogInformation("Creating player");
            var created = await Context.Send(new CreatePlayer {Player = player});
            _logger.LogInformation("Created player {Player}",
                JsonConvert.SerializeObject(created.Player));
            return player;        
        }

        [HttpPut, Route("")]
        public async Task<Player> UpdatePlayer(Player player)
        {
            _logger.LogInformation("Updating player");
            var updated = await Context.Send(new UpdatePlayer { Player = player });
            _logger.LogInformation("Updated player {Player}",
                JsonConvert.SerializeObject(updated.Player));
            return updated.Player;
        }

        [HttpPatch, Route("")]
        public async Task<Player> PatchPlayer(Player player)
        {
            _logger.LogInformation("Updating player");
            var updated = await Context.Send(new UpdatePlayer { Player = player });
            _logger.LogInformation("Updated player {Player}",
                JsonConvert.SerializeObject(updated.Player));
            return updated.Player;
        }

        [HttpDelete, Route("{playerId}")]
        public async Task DeletePlayer(int playerId)
        {
            _logger.LogInformation("Removing player");
            await Context.Send(new RemovePlayer { PlayerId = playerId });
            _logger.LogInformation("Removed player {PlayerId}", playerId);
        }
    }
}
