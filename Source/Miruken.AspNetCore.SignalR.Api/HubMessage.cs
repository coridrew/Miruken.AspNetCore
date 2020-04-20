namespace Miruken.AspNetCore.SignalR.Api
{
    public class HubMessage
    {
        public HubMessage()
        {
        }

        public HubMessage(object payload)
        {
            Payload = payload;
        }

        public object Payload { get; set; }
    }
}
