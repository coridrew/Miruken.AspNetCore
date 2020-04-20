namespace Miruken.AspNetCore.SignalR.Api
{
    using System;

    public class HubClosed : HubEvent
    {
        public Exception Exception { get; set; }
    }
}
