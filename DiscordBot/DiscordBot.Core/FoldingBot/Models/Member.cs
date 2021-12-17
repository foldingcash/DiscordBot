namespace DiscordBot.Core.FoldingBot.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class Member
    {
        [DataMember(Name = "bitcoinAddress")]
        public string BitcoinAddress { get; set; }

        [DataMember(Name = "friendlyName")]
        public string FriendlyName { get; set; }

        [DataMember(Name = "teamNumber")]
        public long TeamNumber { get; set; }

        [DataMember(Name = "userName")]
        public string UserName { get; set; }
    }
}