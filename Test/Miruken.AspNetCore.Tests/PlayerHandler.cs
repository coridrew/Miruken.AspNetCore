namespace Miruken.AspNetCore.Tests
{
    using System.Security.Principal;
    using System.Threading;
    using Callback;
    using Validate;

    [Filter(typeof(ValidateFilter<,>))]
    public class PlayerHandler : Handler
    {
        private static int _id;

        [Handles]
        public PlayerResponse Create(
            CreatePlayer create, IPrincipal principal)
        {
            var player = create.Player;
            player.Id = Interlocked.Increment(ref _id);
            return new PlayerResponse { Player = player };
        }

        [Handles]
        public PlayerResponse Update(
            UpdatePlayer update, IPrincipal principal)
        {
            return new PlayerResponse { Player = update.Player };
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
