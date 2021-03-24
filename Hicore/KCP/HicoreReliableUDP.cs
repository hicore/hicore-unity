using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hicore.KCP
{
    public class HicoreReliableUDP
    {
        private Socket mSocket = null;
        private KCP mKCP = null;
        byte[] buffer = new byte[1024];
        private ByteBuffer mRecvBuffer = ByteBuffer.Allocate(1024 * 32);
        private UInt32 mNextUpdateTime = 0;
        public bool IsConnected { get { return mSocket != null && mSocket.Connected; } }
        public bool WriteDelay { get; set; }
        public bool AckNoDelay { get; set; }
        public IPEndPoint RemoteAddress { get; private set; }
        public IPEndPoint LocalAddress { get; private set; }


        public  Action OnConnected;
        public  Action<String> OnClosed;
        public  Action<String> OnError;

        private CancellationTokenSource _tokenSourceReceiver;
        public Action<string> ReceiveData;

        public async void Connect(string host, int port)
        {

            try
            {
                var endpoint = IPAddress.Parse(host);
                mSocket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                mSocket.Connect(endpoint, port);
                mSocket.BeginConnect(endpoint, port, ConnectCallback, mSocket);
                RemoteAddress = (IPEndPoint)mSocket.RemoteEndPoint;
                LocalAddress = (IPEndPoint)mSocket.LocalEndPoint;
                mKCP = new KCP((uint)(new Random().Next(1, Int32.MaxValue)), rawSend);
                // normal:  0, 40, 2, 1
                // fast:    0, 30, 2, 1
                // fast2:   1, 20, 2, 1
                // fast3:   1, 10, 2, 1
                mKCP.NoDelay(1, 20, 2, 1);
                mKCP.SetStreamMode(true);
                mRecvBuffer.Clear();


                _tokenSourceReceiver = new CancellationTokenSource();

                await Receive();
            }
            catch (Exception ex)
            {
                await CloseAsync();
                OnError(ex.ToString());
            }

        }


        void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                mSocket = (Socket)ar.AsyncState;
                mSocket.EndConnect(ar);
                if (mSocket != null)
                {
                    if (mSocket.Connected)
                    {
                        OnConnected();
                        return;
                    }
                }

                OnClosed("Failed to connect to server...");
                CloseAsync();
            }
            catch (Exception ex)
            {
                OnError(ex.ToString());
            }
        }

        public Task CloseAsync()
        {
            try
            {
                if ((mSocket == null) || (!mSocket.Connected))
                {
                    throw new InvalidOperationException("Attempt to close a socket which is not connected");
                }
                else
                {

                    _tokenSourceReceiver.Cancel();
                    _tokenSourceReceiver.Dispose();
                    mSocket.Shutdown(SocketShutdown.Both);
                    mSocket.Close();
                    mSocket = null;
                    mRecvBuffer.Clear();

                    return Task.CompletedTask;
                }
            }
            catch (Exception ex )
            {
                OnError(ex.ToString());
                
            }

            return null;
        }

        private void rawSend(byte[] data, int length)
        {
            if (mSocket != null)
            {
                mSocket.Send(data, length, SocketFlags.None);
            }
        }

        public int Send(byte[] data, int index, int length)
        {
            if (mSocket == null)
                return -1;

            var waitsnd = mKCP.WaitSnd;
            if (waitsnd < mKCP.SndWnd && waitsnd < mKCP.RmtWnd)
            {

                var sendBytes = 0;
                do
                {
                    var n = Math.Min((int)mKCP.Mss, length - sendBytes);
                    mKCP.Send(data, index + sendBytes, n);
                    sendBytes += n;
                } while (sendBytes < length);

                waitsnd = mKCP.WaitSnd;
                if (waitsnd >= mKCP.SndWnd || waitsnd >= mKCP.RmtWnd || !WriteDelay)
                {
                    mKCP.Flush(false);
                }

                return length;
            }

            return 0;
        }

        public int Recv(byte[] data, int index, int length)
        {
            // 上次剩下的部分
            if (mRecvBuffer.ReadableBytes > 0)
            {
                var recvBytes = Math.Min(mRecvBuffer.ReadableBytes, length);
                Buffer.BlockCopy(mRecvBuffer.RawBuffer, mRecvBuffer.ReaderIndex, data, index, recvBytes);
                mRecvBuffer.ReaderIndex += recvBytes;

                // 读完重置读写指针
                if (mRecvBuffer.ReaderIndex == mRecvBuffer.WriterIndex)
                {
                    mRecvBuffer.Clear();
                }

                return recvBytes;
            }

            if (mSocket == null)
                return -1;

            if (!mSocket.Poll(0, SelectMode.SelectRead))
            {
                return 0;
            }

            var rn = 0;
            try
            {
                rn = mSocket.Receive(mRecvBuffer.RawBuffer, mRecvBuffer.WriterIndex, mRecvBuffer.WritableBytes, SocketFlags.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                rn = -1;
            }

            if (rn <= 0)
            {
                return rn;
            }
            mRecvBuffer.WriterIndex += rn;

            var inputN = mKCP.Input(mRecvBuffer.RawBuffer, mRecvBuffer.ReaderIndex, mRecvBuffer.ReadableBytes, true, AckNoDelay);
            if (inputN < 0)
            {
                mRecvBuffer.Clear();
                return inputN;
            }
            mRecvBuffer.Clear();

            // 读完所有完整的消息
            for (; ; )
            {
                var size = mKCP.PeekSize();
                if (size <= 0) break;

                mRecvBuffer.EnsureWritableBytes(size);

                var n = mKCP.Recv(mRecvBuffer.RawBuffer, mRecvBuffer.WriterIndex, size);
                if (n > 0) mRecvBuffer.WriterIndex += n;
            }

            // 有数据待接收
            if (mRecvBuffer.ReadableBytes > 0)
            {
                return Recv(data, index, length);
            }

            return 0;
        }

        public void Update()
        {
            if (mSocket == null)
                return;

            if (0 == mNextUpdateTime || mKCP.CurrentMS >= mNextUpdateTime)
            {
                mKCP.Update();
                mNextUpdateTime = mKCP.Check();
            }
        }


        public async Task Receive()
        {
            while (true)
            {
                if (!_tokenSourceReceiver.IsCancellationRequested)
                {
                    Task recevirTaslk = Task.Run(() => ReceiverLoop(), _tokenSourceReceiver.Token);
                    await recevirTaslk;
                }
                else
                {
                    break;
                }

            }
        }

        public void ReceiverLoop()
        {
            Update();

            var received = Recv(buffer, 0, buffer.Length);

            if (received > 0)
            {
                var resp = Encoding.UTF8.GetString(buffer, 0, received);
                ReceiveData(resp);
            }
            else if (received == 0)
            {
                Thread.Sleep(10);
            }
            else if (received < 0)
            {
                //Receive Message failed.
            }

        }

    }
}
