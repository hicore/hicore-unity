using System;

namespace Hicore.Arguments
{
    public class PingEventArgs : EventArgs
    {
        public int SentAtTick { get; private set; }
        public int ReceivedAtTick { get; private set; }

        internal PingEventArgs(int sent, int received) : base()
        {
            SentAtTick = sent;
            ReceivedAtTick = received;
        }
        public int GetPingInMilliseconds() 
        {
            var getMs = (ReceivedAtTick - SentAtTick);
            return getMs;
        }
    }
}
