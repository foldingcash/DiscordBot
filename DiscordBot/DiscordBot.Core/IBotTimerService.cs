namespace DiscordBot.Core
{
    using System;

    public interface IBotTimerService : IDisposable
    {
        void Start();

        void Stop();
    }
}