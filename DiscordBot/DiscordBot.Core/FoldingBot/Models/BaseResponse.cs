namespace DiscordBot.Core.FoldingBot.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class BaseResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }
    }
}