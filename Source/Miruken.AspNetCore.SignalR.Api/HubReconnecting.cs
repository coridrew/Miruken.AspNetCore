namespace Miruken.AspNetCore.SignalR.Api
{
    using System;

    public class HubReconnecting : HubEvent
    {
        public Exception Exception { get; set; }
    }
}
