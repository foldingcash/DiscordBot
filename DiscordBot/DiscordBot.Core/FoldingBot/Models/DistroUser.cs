namespace DiscordBot.Core.FoldingBot.Models
{
    public class DistroUser
    {
        public decimal Amount { get; set; }

        public string BitcoinAddress { get; set; }

        public string CashTokensAddress { get; set; }

        public long PointsGained { get; set; }

        public long WorkUnitsGained { get; set; }
    }
}