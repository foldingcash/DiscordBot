namespace DiscordBot.Core.FoldingBot.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class ApiError
    {
        [DataMember(Name = "errorCode")]
        public string ErrorCode { get; set; }

        [DataMember(Name = "errorMessage")]
        public string ErrorMessage { get; set; }
    }
}