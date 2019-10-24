namespace DiscordBot.Core.Models
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class CoinMarketCapMarketValueResponse
    {
        [DataMember(Name = "available_supply")]
        public string AvailableSupply { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "last_updated")]
        public string LastUpdated { get; set; }

        [IgnoreDataMember]
        public DateTimeOffset? LastUpdatedDateTime
        {
            get
            {
                bool parsed = long.TryParse(LastUpdated, out long lastUpdated);
                DateTimeOffset lastUpdatedDateTime = DateTimeOffset.FromUnixTimeSeconds(lastUpdated);
                return parsed ? lastUpdatedDateTime as DateTimeOffset? : null;
            }
        }

        [DataMember(Name = "market_cap_usd")]
        public string MarketCapInUsd { get; set; }

        [DataMember(Name = "max_supply")]
        public string MaxSupply { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "percent_change_24h")]
        public string PercentChangeLastDay { get; set; }

        [DataMember(Name = "percent_change_1h")]
        public string PercentChangeLastHour { get; set; }

        [DataMember(Name = "percent_change_7d")]
        public string PercentChangeLastWeek { get; set; }

        [DataMember(Name = "price_btc")]
        public string PriceInBtc { get; set; }

        [DataMember(Name = "price_usd")]
        public string PriceInUsd { get; set; }

        [DataMember(Name = "rank")]
        public string Rank { get; set; }

        [DataMember(Name = "symbol")]
        public string Symbol { get; set; }

        [DataMember(Name = "total_supply")]
        public string TotalSupply { get; set; }

        [DataMember(Name = "24h_volume_usd")]
        public string VolumeUsdLastDay { get; set; }
    }
}