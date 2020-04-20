namespace Miruken.AspNetCore.SignalR.Api
{
    public abstract class HubEvent
    {
        public HubConnectionInfo ConnectionInfo { get; set; }
    }
}
