namespace DiscordBot.Core.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Method)]
    public class AdminOnlyAttribute : Attribute
    {
    }
}