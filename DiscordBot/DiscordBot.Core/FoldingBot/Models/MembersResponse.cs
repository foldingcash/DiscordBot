namespace DiscordBot.Core.FoldingBot.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    internal class MembersResponse : BaseResponse
    {
        [DataMember(Name = "errorCount")]
        public int? ErrorCount { get; set; }

        [DataMember(Name = "errors")]
        public IList<ApiError> Errors { get; set; }

        [DataMember(Name = "firstErrorCode")]
        public string FirstErrorCode { get; set; }

        [DataMember(Name = "memberCount")]
        public int? MemberCount { get; set; }

        [DataMember(Name = "members")]
        public IList<Member> Members { get; set; }
    }
}