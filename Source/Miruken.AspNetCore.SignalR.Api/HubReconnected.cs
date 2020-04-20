namespace Miruken.AspNetCore.SignalR.Api
{
    public class HubReconnected : HubEvent
    {
        public string NewConnectionId { get; set; }
    }
}
