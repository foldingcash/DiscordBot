namespace DiscordBot.Core.FoldingBot.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class BaseResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }
    }
}