namespace DiscordBot.Core.FoldingBot.Models
{
    using System.Collections.Generic;

    public class MembersResponse : BaseResponse
    {
        public int? ErrorCount { get; set; }

        public IList<ApiError> Errors { get; set; }

        public string FirstErrorCode { get; set; }

        public int? MemberCount { get; set; }

        public IList<Member> Members { get; set; }
    }
}