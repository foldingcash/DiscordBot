namespace DiscordBot.Core.FoldingBot.Models
{
    using System.Collections.Generic;

    public class HealthResponse
    {
        public Dictionary<string, HealthEntry> Entries { get; set; }

        public string Status { get; set; }

        public string TotalDuration { get; set; }
    }

    public class HealthEntry
    {
        public Dictionary<string, string> Data { get; set; }

        public string Description { get; set; }

        public string Duration { get; set; }

        public string Exception { get; set; }

        public string Status { get; set; }

        public List<string> Tags { get; set; }
    }
}