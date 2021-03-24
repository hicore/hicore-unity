using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Hicore.KCP
{
    public delegate void ConnectedHandler(IAsyncRudpClient a);

    public delegate void ClientMessageReceivedHandler(IAsyncRudpClient a, string msg);

    public delegate void ClientMessageSubmittedHandler(IAsyncRudpClient a, bool close);

    public sealed class AsyncRudpClient : IAsyncRudpClient
    {
        //private const ushort Port = 0000;

        private Socket listener;
        private bool close;

        private readonly ManualResetEvent connected = new ManualResetEvent(false);
        private readonly ManualResetEvent sent = new ManualResetEvent(false);
        private readonly ManualResetEvent received = new ManualResetEvent(false);

        public event ConnectedHandler Connected;

        public event ClientMessageReceivedHandler MessageReceived;

        public event ClientMessageSubmittedHandler MessageSubmitted;

        private KCP mKCP = null;

        private ByteBuffer mRecvBuffer = ByteBuffer.Allocate(1024 * 32);
        private UInt32 mNextUpdateTime = 0;

        public bool WriteDelay { get; set; }
        public bool AckNoDelay { get; set; }

        public IPEndPoint RemoteAddress { get; private set; }
        public IPEndPoint LocalAddress { get; private set; }

        public Action OnConnected;
        public Action<String> OnClosed;
        public Action<String> OnError;

        private CancellationTokenSource _tokenSourceReceiver;
        public Action<string> ReceiveData;


        byte[] buffer = new byte[1024];
        public async void StartClient(string host, int port)
        {
            Uri gameServerUri = new Uri(host);
           
            IPHostEntry hostEntry = Dns.GetHostEntry(gameServerUri.Host);
            if (hostEntry.AddressList.Length == 0)
            {
                throw new Exception("Unable to resolve host: " + gameServerUri.Host);
            }
            var endpoint = hostEntry.AddressList[0];

            try
            {
                this.listener = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                this.listener.BeginConnect(endpoint, port, this.OnConnectCallback, this.listener);
                //this.connected.WaitOne();
                RemoteAddress = (IPEndPoint)this.listener.RemoteEndPoint;
                LocalAddress = (IPEndPoint)this.listener.LocalEndPoint;

                mKCP = new KCP((uint)(new Random().Next(1, Int32.MaxValue)), Send);
                // normal:  0, 40, 2, 1
                // fast:    0, 30, 2, 1
                // fast2:   1, 20, 2, 1
                // fast3:   1, 10, 2, 1
                mKCP.NoDelay(1, 10, 2, 1);
                mKCP.WndSize(128, 128);
                mKCP.SetStreamMode(true);
                mRecvBuffer.Clear();

                _tokenSourceReceiver = new CancellationTokenSource();


                await Receive();

                var connectedHandler = this.Connected;

                if (connectedHandler != null)
                {
                    connectedHandler(this);
                }
            }
            catch (Exception ex)//SocketException
            {
                await CloseAsync();
                OnError(ex.ToString());
            }
        }
        public const int SALT_SIZE = 24; // size in bytes
        public const int HASH_SIZE = 24; // size in bytes
        public const int ITERATIONS = 100000; // number of pbkdf2 iterations

        public static byte[] CreateHash(string input)
        {
            // Generate a salt
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            byte[] salt = new byte[SALT_SIZE];
            provider.GetBytes(salt);

            // Generate the hash
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(input, salt, ITERATIONS);
            return pbkdf2.GetBytes(HASH_SIZE);
        }

        public bool IsConnected()
        {
            return !(this.listener.Poll(1000, SelectMode.SelectRead) && this.listener.Available == 0);
        }

        private void OnConnectCallback(IAsyncResult result)
        {
            var server = (Socket)result.AsyncState;

            try
            {
                server.EndConnect(result);
                this.connected.Set();
                OnConnected.Invoke();

            }
            catch (SocketException)
            {
            }
        }

        #region Receive data

     /*   public void Receive()
        {
            var state = new StateObject(this.listener);

            state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, this.ReceiveCallback,
                state);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            var state = (IStateObject)result.AsyncState;
            var receive = state.Listener.EndReceive(result);

            if (receive > 0)
            {
                state.Append(Encoding.UTF8.GetString(state.Buffer, 0, receive));
            }

            if (receive == state.BufferSize)
            {
                state.Listener.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, this.ReceiveCallback,
                    state);
            }
            else
            {
                var messageReceived = this.MessageReceived;

                if (messageReceived != null)
                {
                    messageReceived(this, state.Text);
                }

                state.Reset();
                this.received.Set();
            }
        }*/

        #endregion

        #region Send data

        public void Send(byte[] data, int length)
        {
            if (!this.IsConnected())
            {
                throw new Exception("Destination socket is not connected.");
            }

            this.listener.BeginSend(data, 0, length, SocketFlags.None, this.SendCallback, this.listener);
        }

        private void SendCallback(IAsyncResult result)
        {
            try
            {
                var resceiver = (Socket)result.AsyncState;

                resceiver.EndSend(result);


            }
            catch (SocketException)
            {
                // TODO:
            }
            catch (ObjectDisposedException)
            {
                // TODO;
            }

            var messageSubmitted = this.MessageSubmitted;

            if (messageSubmitted != null)
            {
                messageSubmitted(this, this.close);

            }

            this.sent.Set();
        }

        #endregion

        public Task CloseAsync()
        {
            try
            {
                if ((this.listener == null) || (!this.listener.Connected))
                {
                    throw new InvalidOperationException("Attempt to close a socket which is not connected");
                }
                else
                {

                    _tokenSourceReceiver.Cancel();
                    _tokenSourceReceiver.Dispose();
                    this.listener.Shutdown(SocketShutdown.Both);
                    this.listener.Close();
                    this.listener = null;
                    mRecvBuffer.Clear();

                    return Task.CompletedTask;
                }
            }
            catch (Exception ex)
            {
                OnError(ex.ToString());

            }

            return null;
        }

        public void Dispose()
        {
            this.connected.Dispose();
            this.sent.Dispose();
            this.received.Dispose();
            this.CloseAsync();
        }

        public int Send(byte[] data, int index, int length)
        {
            if (this.listener == null)
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
            // The remaining part from last time
            if (mRecvBuffer.ReadableBytes > 0)
            {
                var recvBytes = Math.Min(mRecvBuffer.ReadableBytes, length);
                Buffer.BlockCopy(mRecvBuffer.RawBuffer, mRecvBuffer.ReaderIndex, data, index, recvBytes);
                mRecvBuffer.ReaderIndex += recvBytes;
                // Reset the read and write pointer after reading
                if (mRecvBuffer.ReaderIndex == mRecvBuffer.WriterIndex)
                {
                    mRecvBuffer.Clear();
                }

                return recvBytes;
            }

            if (this.listener == null)
                return -1;

            if (!this.listener.Poll(0, SelectMode.SelectRead))
            {
                return 0;
            }

            var rn = 0;
            try
            {
                rn = this.listener.Receive(mRecvBuffer.RawBuffer, mRecvBuffer.WriterIndex, mRecvBuffer.WritableBytes,
                    SocketFlags.None);
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

            var inputN = mKCP.Input(mRecvBuffer.RawBuffer, mRecvBuffer.ReaderIndex, mRecvBuffer.ReadableBytes, true,
                AckNoDelay);
            if (inputN < 0)
            {
                mRecvBuffer.Clear();
                return inputN;
            }

            mRecvBuffer.Clear();

            // Read all the complete messages
            for (; ; )
            {
                var size = mKCP.PeekSize();
                if (size <= 0) break;

                mRecvBuffer.EnsureWritableBytes(size);

                var n = mKCP.Recv(mRecvBuffer.RawBuffer, mRecvBuffer.WriterIndex, size);
                if (n > 0) mRecvBuffer.WriterIndex += n;
            }

            // There is data to receive
            if (mRecvBuffer.ReadableBytes > 0)
            {
                return Recv(data, index, length);
            }

            return 0;
        }

        public void Update()
        {
            if (this.listener == null)
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
                Update();
                
                if (!_tokenSourceReceiver.IsCancellationRequested)
                {
                    await Task.Run(() => ReceiverLoop(), _tokenSourceReceiver.Token);
                }
                else
                {
                    break;
                }

            }
        }

        public void ReceiverLoop()
        {
            

            var state = new StateObject(this.listener);

            var recv = Recv(state.Buffer, 0, state.BufferSize);

            if (recv > 0)
            {
                var resp = Encoding.UTF8.GetString(state.Buffer, 0, recv);

                ReceiveData(resp);
            }
            else if (recv == 0)
            {
                Thread.Sleep(10);
            }


        }
    }
}
