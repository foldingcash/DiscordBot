namespace DiscordBot.Core.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    internal class DistroResponse : BaseResponse
    {
        [DataMember(Name = "distro")]
        public IList<DistroUser> Distro { get; set; }

        [DataMember(Name = "distroCount")]
        public int? DistroCount { get; set; }

        [DataMember(Name = "errorCount")]
        public int? ErrorCount { get; set; }

        [DataMember(Name = "errors")]
        public IList<ApiError> Errors { get; set; }

        [DataMember(Name = "firstErrorCode")]
        public int FirstErrorCode { get; set; }

        [DataMember(Name = "totalDistro")]
        public decimal? TotalDistro { get; set; }

        [DataMember(Name = "totalPoints")]
        public long? TotalPoints { get; set; }

        [DataMember(Name = "totalWorkUnits")]
        public long? TotalWorkUnits { get; set; }
    }
}