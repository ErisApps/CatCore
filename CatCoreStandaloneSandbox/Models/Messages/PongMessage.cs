namespace CatCoreStandaloneSandbox.Models.Messages
{
    internal class PongMessage : MessageBase
    {
        public PongMessage()
        {
            Type = "PONG";
        }
    }
}