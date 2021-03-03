namespace CatCoreStandaloneSandbox.Models.Messages
{
    internal class ReconnectMessage : MessageBase
    {
        public ReconnectMessage()
        {
            Type = "RECONNECT";
        } 
    }
}