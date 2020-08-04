namespace DiscordBot.Core.Models
{
    using System.Runtime.Serialization;

    internal class BaseResponse
    {
        [DataMember(Name = "success")]
        public bool Success { get; set; }
    }
}