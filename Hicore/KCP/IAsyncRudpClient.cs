using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hicore.KCP
{
    public interface IAsyncRudpClient : IDisposable
    {
        event ConnectedHandler Connected;

        event ClientMessageReceivedHandler MessageReceived;

        event ClientMessageSubmittedHandler MessageSubmitted;

        void StartClient(string host, int port);

        bool IsConnected();

        Task Receive();

        void Send(byte[] data, int length);
    }
}
