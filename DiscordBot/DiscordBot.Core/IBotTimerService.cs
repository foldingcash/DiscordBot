namespace DiscordBot.Core
{
    using System;

    public interface IBotTimerService : IDisposable
    {
        void Close();

        void Start();

        void Stop();
    }
}