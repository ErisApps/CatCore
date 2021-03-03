namespace CatCoreStandaloneSandbox.Models.Messages
{
    internal class PingMessage : MessageBase
    {
        public PingMessage()
        {
            Type = "PING";
        }
    }
}