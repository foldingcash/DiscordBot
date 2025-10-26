namespace DiscordBot.Core.FoldingBot.Models
{
    using System.Runtime.Serialization;

    [DataContract]
    public class DistroUser
    {
        [DataMember(Name = "amount")]
        public decimal Amount { get; set; }

        [DataMember(Name = "bitcoinAddress")]
        public string BitcoinAddress { get; set; }

        [DataMember(Name = "cashTokensAddress")]
        public string CashTokensAddress { get; set; }

        [DataMember(Name = "pointsGained")]
        public long PointsGained { get; set; }

        [DataMember(Name = "workUnitsGained")]
        public long WorkUnitsGained { get; set; }
    }
}