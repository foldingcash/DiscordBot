namespace DiscordBot.Core.FoldingBot.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class DistroResponse : BaseResponse
    {
        public IList<DistroUser> Distro { get; set; }

        public int? DistroCount { get; set; }

        public DateTime End => DateTime.Parse(EndDateTime, CultureInfo.InvariantCulture);

        public string EndDateTime { get; set; }

        public int? ErrorCount { get; set; }

        public IList<ApiError> Errors { get; set; }

        public int FirstErrorCode { get; set; }

        public DateTime Start => DateTime.Parse(StartDateTime, CultureInfo.InvariantCulture);

        public string StartDateTime { get; set; }

        public decimal? TotalDistro { get; set; }

        public long? TotalPoints { get; set; }

        public long? TotalWorkUnits { get; set; }
    }
}