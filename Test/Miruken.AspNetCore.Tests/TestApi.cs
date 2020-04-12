namespace Miruken.AspNetCore.Tests
{
    using System;
    using Api;
    using FluentValidation;
    using Microsoft.AspNetCore.Mvc;

    public class Person
    {
        public DateTime DOB { get; set; }
    }

    public class Player
    {
        public int    Id     { get; set; }
        public string Name   { get; set; }
        public Person Person { get; set; }
    }

    public class GetPlayer : IRequest<PlayerResponse>
    {
        public int PlayerId { get; set; }
    }

    public class CreatePlayer : IRequest<PlayerResponse>
    {
        public Player Player { get; set; }
    }

    public class PlayerCreated
    {
        public Player Player { get; set; }
    }

    public class UpdatePlayer : IRequest<PlayerResponse>
    {
        public Player Player { get; set; }
    }

    public class PlayerUpdated
    {
        public Player Player { get; set; }
    }

    public class RemovePlayer
    {
        public int PlayerId { get; set; }
    }

    public class PlayerResponse
    {
        public Player Player { get; set; }
    }

    public class RenderPlayer : IRequest<IActionResult>
    {
        public int PlayerId { get; set; }
    }

    public class CreatePlayerIntegrity : AbstractValidator<CreatePlayer>
    {
        public CreatePlayerIntegrity()
        {
            RuleFor(cp => cp.Player).NotNull();
            When(p => p.Player != null, () =>
            {
                RuleFor(p => p.Player.Id).Equal(0);
                RuleFor(p => p.Player.Name).NotEmpty();
            });
        }
    }

    public class UpdatePlayerIntegrity : AbstractValidator<UpdatePlayer>
    {
        public UpdatePlayerIntegrity()
        {
            RuleFor(up => up.Player).NotNull()
                .DependentRules(() =>
                {
                    RuleFor(p => p.Player.Id).GreaterThan(0);
                });
        }
    }

    public class RemovePlayerIntegrity : AbstractValidator<RemovePlayer>
    {
        public RemovePlayerIntegrity()
        {
            RuleFor(rp => rp.PlayerId).GreaterThan(0);
        }
    }
}
