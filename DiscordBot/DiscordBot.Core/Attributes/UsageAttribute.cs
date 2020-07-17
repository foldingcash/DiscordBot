namespace DiscordBot.Core.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class UsageAttribute : Attribute
    {
        public UsageAttribute(string usage)
        {
            Usage = usage;
        }

        public string Usage { get; }
    }
}