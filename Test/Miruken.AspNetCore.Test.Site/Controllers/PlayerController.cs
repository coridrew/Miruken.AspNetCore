namespace Miruken.AspNetCore.Test.Site.Controllers
{
    using AspNetCore;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class Player 
    {
        public int    Id   { get; set; }
        public string Name { get; set; }
    }

    [ApiController,
     Route("[controller]")]
    public class PlayerController : ContextualControllerBase
    {
        private readonly ILogger<PlayerController> _logger;

        public PlayerController(ILogger<PlayerController> logger)
        {
            _logger = logger;
        }

        [HttpGet, Route("{id}")]
        public Player GetPlayer(int id)
        {
            _logger.LogInformation("Getting player {0}", id);
            return new Player { Id = id, Name = "Christiano Ronaldo" };
        }

        [HttpPost, Route("")]
        public Player CreatePlayer(Player player)
        {
            _logger.LogInformation("Creating player");
            player.Id = 1;
            _logger.LogInformation("Created player {0}", JsonConvert.SerializeObject(player));
            _logger.LogInformation("Created player", JsonConvert.SerializeObject(player));
            _logger.LogInformation("Created player", player);
            return player;        
        }

        [HttpPut, Route("")]
        public Player UpdatePlayer(Player player)
        {
            return player;
        }

        [HttpPatch, Route("")]
        public Player PatchPlayer(Player player)
        {
            return player;
        }

        [HttpDelete, Route("{id}")]
        public void DeletePlayer(int id)
        {
        }
    }
}
