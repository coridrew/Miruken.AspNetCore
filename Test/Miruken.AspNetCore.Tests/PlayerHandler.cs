namespace Miruken.AspNetCore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Callback;
    using Validate;

    [Validate]
    public class PlayerHandler : Handler
    {
        private static int _id;

        private readonly Dictionary<int, Player> _players = new Dictionary<int, Player>();

        [Handles]
        public PlayerResponse Get(GetPlayer get)
        {
            if (_players.TryGetValue(get.PlayerId, out var player))
                return new PlayerResponse { Player = player };
            throw new InvalidOperationException($"Player {get.PlayerId} not found");
        }

        [Handles]
        public PlayerResponse Create(CreatePlayer create)
        {
            var player = create.Player;
            player.Id = Interlocked.Increment(ref _id);
            _players[player.Id] = player;
            return new PlayerResponse { Player = player };
        }

        [Handles]
        public PlayerResponse Update(UpdatePlayer update)
        {
            var updatedPlayer = update.Player 
                ?? throw new ArgumentException("Missing player");
            if (_players.TryGetValue(updatedPlayer.Id, out var player))
            {
                if (updatedPlayer.Name != null)
                    player.Name = updatedPlayer.Name;
                if (updatedPlayer.Person != null)
                    player.Person = updatedPlayer.Person;
                return new PlayerResponse { Player = update.Player };
            }
            throw new InvalidOperationException($"Player {updatedPlayer.Id} not found");
        }

        [Handles]
        public void Created(PlayerCreated created)
        {
        }

        [Handles]
        public void Updated(PlayerUpdated updated)
        {
        }
    }
}
