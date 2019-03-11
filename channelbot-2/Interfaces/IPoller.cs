using System.Timers;

namespace channelbot_2.Interfaces
{
    internal interface IPoller
    {
        int PollInterval { get; set; }
        void OnSetup();
        void OnPoll(object source, ElapsedEventArgs e);
    }
}