using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Hicore.Arguments;
using Hicore.Parsers;


namespace Hicore
{

    public class HicoreSocket
    {


        public HicoreSocket(string uri, int port) : this(new Uri(uri), port) { }

        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        readonly Uri _uri;
        private ClientWebSocket _socket;
        readonly UrlConverter _urlConverter;
        readonly string _namespace;
        private CancellationTokenSource _tokenSource;
        private int _packetId;
        public Dictionary<int, EventHandler> Callbacks { get; }

        public int EIO { get; set; } = 3;
        public TimeSpan ConnectTimeout { get; set; }
        public Dictionary<string, string> Parameters { get; set; }

        public event Action OnConnected;
        public event Action<PingEventArgs> OnPing;
        public event Action<ResponseArgs> OnError;
        public event Action<ServerCloseReason> OnClosed;
        public event Action<string, ResponseArgs> UnhandledEvent;
        public event Action<string, ResponseArgs> OnReceivedEvent;
        public event Action<string> CustomIncomingEvent; //todo 

        public Dictionary<string, EventHandler> EventHandlers { get; }

        public SocketState State { get; private set; }
        private ServerCloseReason serverCloseReason;

        private Thread _receiverLoop;
        private Thread _senderLoop;

        private int pingRequest = 0;
        private int pingRequestAt = 0;
        private int pingRequestDeadline = 0;
        private int pongTimeoutDeadline = 0;

        private TimeSpan PingInterval { get; set; }
        private TimeSpan PingTimeout { get; set; }


        //public event EventHandler<IncomingEventEventArgs> IncomingEvent;


        public HicoreSocket() { }


        public HicoreSocket(Uri uri, int port)
        {
            var uriBuilder = new UriBuilder(uri);
            uriBuilder.Port = port;

            if (uriBuilder.Scheme == "https" || uriBuilder.Scheme == "http" || uriBuilder.Scheme == "wss" || uriBuilder.Scheme == "ws")
            {
                _uri = uriBuilder.Uri;
            }
            else
            {
                throw new ArgumentException("Unsupported protocol. It must be one of these protocols(https-http-wss-ws)");
            }
            EventHandlers = new Dictionary<string, EventHandler>();
            Callbacks = new Dictionary<int, EventHandler>();
            _urlConverter = new UrlConverter();
            if (_uri.AbsolutePath != "/")
            {
                _namespace = _uri.AbsolutePath + ',';
            }
            _packetId = -1;
            ConnectTimeout = TimeSpan.FromSeconds(30);
            PingInterval = TimeSpan.FromSeconds(3);
            PingTimeout = TimeSpan.FromSeconds(4); // it should be bigger than PingInterval


        }

        public Task ConnectAsync()
        {

            _tokenSource = new CancellationTokenSource();
            if (!Parameters.ContainsKey("type"))
            {
                Parameters.Add("type", "client");
            }
            Uri wsUri = _urlConverter.HttpToWs(_uri, EIO.ToString(), Parameters);
            if (_socket != null)
            {
                _socket.Dispose();
            }
            _socket = new ClientWebSocket();
            bool executed = _socket.ConnectAsync(wsUri, CancellationToken.None).Wait(ConnectTimeout);
            if (!executed)
            {
                throw new TimeoutException();
            }

            _receiverLoop = new Thread(ReceiverLoop);
            _receiverLoop.Start(_tokenSource.Token);

            _senderLoop = new Thread(SenderLoop);
            _senderLoop.Start(_tokenSource.Token);


            return Task.CompletedTask;
        }

        public Task CloseAsync()
        {
            if (_socket == null)
            {
                throw new InvalidOperationException("Close failed, must connect first.");
            }
            else
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _socket.Abort();
                _receiverLoop.Abort();
                _senderLoop.Abort();
                _socket.Dispose();
                WakeSenderLoop();
                State = SocketState.Closed;
                _socket = null;
                if (serverCloseReason != ServerCloseReason.SocketClosedByClient)
                {
                    OnClosed?.Invoke(serverCloseReason);
                }
                else
                {
                    OnClosed?.Invoke(ServerCloseReason.SocketClosedByClient);
                }
                // SocketClosedByClient
                return Task.CompletedTask;
            }
        }


        ConcurrentQueue<byte[]> senderQueue = new ConcurrentQueue<byte[]>();

        /// <summary>
        /// Send a WebSocket packet
        /// </summary>
        /// <param name="data">packet. Could be null to wake-up the sender loop</param>

        private void WakeSenderLoop()
        {
            SendRawData(null);
        }

        public void SendRawData(byte[] data)
        {
            // Currently does not check for memory overflow
            senderQueue.Enqueue(data);
            lock (senderQueue)
            {
                Monitor.Pulse(senderQueue);
            }
        }


        private byte[] ExtractSenderQueue()
        {
            byte[] data;
            lock (senderQueue)
            {
                while (!senderQueue.TryDequeue(out data))
                {
                    Monitor.Wait(senderQueue);
                }
            }

            return data;
        }

        private async void SenderLoop(object obj)
        {
            byte[] data;
            while (_socket.State == WebSocketState.Open)
            {
                data = ExtractSenderQueue();
                if (_socket.State != WebSocketState.Open)
                    break;
                try
                {


                    if (Interlocked.Exchange(ref pingRequest, 0) == 1)
                    {
                        pingRequestAt = Environment.TickCount;
                        await _socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("2")),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }

                    if (data == null) continue;
                    await _socket.SendAsync(new ArraySegment<byte>(data),
                                WebSocketMessageType.Text, true, CancellationToken.None);

                    if (Interlocked.Exchange(ref pingRequest, 0) == 1)
                    {
                        pingRequestAt = Environment.TickCount;
                        await _socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("2")),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                catch (WebSocketException)
                {
                    // WebSocket is dead, quitting sliently. 
                    // Raise disconnect event in Receiver Loop
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // WebSocket is dead, quitting sliently. 
                    // Raise disconnect event in Receiver Loop
                    break;
                }
            }
        }

        private async void ReceiverLoop(object obj)
        {
            WebSocketReceiveResult r;
            var buffer = new byte[ReceiveChunkSize];
            var bufferSegment = new ArraySegment<byte>(buffer);



            pingRequestDeadline = Environment.TickCount + (int)PingInterval.TotalMilliseconds;
            pongTimeoutDeadline = Environment.TickCount + (int)PingTimeout.TotalMilliseconds;
            Task<WebSocketReceiveResult> tReceive = null;
            Task tPing = null;
            while (_socket.State == WebSocketState.Open)
            {

                try
                {
                    if (tReceive == null)
                        tReceive = _socket.ReceiveAsync(bufferSegment, CancellationToken.None);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                if (tPing == null)
                    tPing = Task.Delay(Math.Max(0, Math.Min(pingRequestDeadline - Environment.TickCount, pongTimeoutDeadline - Environment.TickCount)));

                var t = await Task.WhenAny(tReceive, tPing);

                if (t == tReceive)
                {
                    try
                    {
                        r = await tReceive;
                        tReceive = null;
                    }
                    catch (WebSocketException)
                    {
                        // Disconnection?
                        break;
                    }

                    switch (r.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            if (r.Count > 0)
                                // Defalut engine.io protocol
                                switch (buffer[0])
                                {
                                    case (byte)'3': // Server Pong
                                        pongTimeoutDeadline = Environment.TickCount + (int)PingTimeout.TotalMilliseconds;
                                        PingEventArgs pingArgs = new PingEventArgs(pingRequestAt, Environment.TickCount);
                                        OnPing?.Invoke(pingArgs);
                                        break;

                                    case (byte)'4': // Message
                                        if (r.Count > 1)
                                            // Defalut socket.io protocol
                                            switch (buffer[1])
                                            { // Ignore All of them. it's just for understanding !
                                                case (byte)'0': // Connect
                                                case (byte)'1': // Disconnect
                                                    // Ignored
                                                    break;
                                                case (byte)'2': // Event
                                                case (byte)'3': // Ack
                                                case (byte)'4': // Error
                                                case (byte)'5': // Binary_Event
                                                case (byte)'6': // Binary_Ack
                                                    // Ignored
                                                    break;
                                            }

                                        // Listen to message
                                        var builder = new StringBuilder();
                                        string str = Encoding.UTF8.GetString(buffer, 0, r.Count);
                                        builder.Append(str);

                                        while (!r.EndOfMessage)
                                        {
                                            r = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _tokenSource.Token);
                                            str = Encoding.UTF8.GetString(buffer, 0, r.Count);
                                            builder.Append(str);
                                        }

                                        var parser = new ResponseTextParser(_namespace, this)
                                        {
                                            Text = builder.ToString()
                                        };
                                        await parser.ParseAsync();


                                        break;
                                }

                            break;
                        case WebSocketMessageType.Binary:
                        case WebSocketMessageType.Close:
                        default:
                            // Nothing to handle
                            break;
                    }
                }
                else
                {
                    if (Environment.TickCount - pingRequestDeadline >= 0)
                    {
                        if (Interlocked.CompareExchange(ref pingRequest, 1, 0) == 0)
                        {
                            pingRequest = 1;
                            WakeSenderLoop();
                            pingRequestDeadline = Environment.TickCount + (int)PingInterval.TotalMilliseconds;
                            pongTimeoutDeadline = Environment.TickCount + (int)PingTimeout.TotalMilliseconds;
                        }

                    }
                    if (Environment.TickCount - pongTimeoutDeadline >= 0)
                    {
                        // Ping timeout
                        try
                        {
                            await _socket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Ping timeout", CancellationToken.None);
                        }
                        catch (WebSocketException) { }
                        catch (ObjectDisposedException) { }
                        break;
                    }
                }

            }

            serverCloseReason = ServerCloseReason.SocketAborted;
            await CloseAsync();
        }


        private void Listen()
        {
            // Listen State
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(500);
                    if (_socket.State == WebSocketState.Aborted || _socket.State == WebSocketState.Closed)
                    {
                        if (State != SocketState.Closed)
                        {
                            State = SocketState.Closed;
                            _tokenSource.Cancel();
                            OnClosed?.Invoke(ServerCloseReason.SocketAborted);
                        }
                    }
                }
            }, _tokenSource.Token);

            // Listen Message
            Task.Factory.StartNew(async () =>
            {
                var buffer = new byte[ReceiveChunkSize];
                while (true)
                {
                    if (_socket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _tokenSource.Token);
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var builder = new StringBuilder();
                            string str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            builder.Append(str);

                            while (!result.EndOfMessage)
                            {
                                result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _tokenSource.Token);
                                str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                                builder.Append(str);
                            }

                            var parser = new ResponseTextParser(_namespace, this)
                            {
                                Text = builder.ToString()
                            };
                            await parser.ParseAsync();
                        }
                    }
                }
            }, _tokenSource.Token);
        }

        private void SendMessage(string text)
        {
            if (_socket.State == WebSocketState.Open)
            {
                var messageBuffer = Encoding.UTF8.GetBytes(text);
                var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

                for (var i = 0; i < messagesCount; i++)
                {
                    int offset = SendChunkSize * i;
                    int count = SendChunkSize;
                    bool isEndOfMessage = (i + 1) == messagesCount;

                    if ((count * (i + 1)) > messageBuffer.Length)
                    {
                        count = messageBuffer.Length - offset;
                    }

                    _socket.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, isEndOfMessage, _tokenSource.Token);
                }
            }
        }

        public void SendEventPayload(string payload, string nsp = "/")
        {
            SendRawData(Encoding.UTF8.GetBytes(
                "42" +
                (!string.IsNullOrWhiteSpace(nsp) && nsp != "/" ? nsp + "," : string.Empty) + payload
                ));
        }

        public Task InvokeConnectedAsync()
        {
            State = SocketState.Connected;
            OnConnected?.Invoke();
            return Task.CompletedTask;
        }

        public async Task InvokeClosedAsync()
        {
            if (State != SocketState.Closed && _socket != null)
            {
                State = SocketState.Closed;
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _tokenSource.Token);
                _tokenSource.Cancel();
                serverCloseReason = ServerCloseReason.SocketClosedByServer;
                OnClosed?.Invoke(serverCloseReason);
            }
        }

        public async Task InvokeOpenedAsync(OpenedArgs args)
        {
            await Task.Factory.StartNew(async () =>
            {
                if (_namespace != null)
                {
                    SendMessage("40" + _namespace);
                }
                State = SocketState.Connected;
                while (true)
                {
                    if (State == SocketState.Connected)
                    {
                        await Task.Delay(args.PingInterval);
                        SendMessage(((int)EngineIOProtocol.Ping).ToString());
                    }
                    else
                    {
                        break;
                    }
                }
            });
        }

        public Task InvokeUnhandledEvent(string eventName, ResponseArgs args)
        {
            UnhandledEvent?.Invoke(eventName, args);
            return Task.CompletedTask;
        }

        public Task InvokeReceivedEvent(string eventName, ResponseArgs args)
        {
            OnReceivedEvent?.Invoke(eventName, args);
            return Task.CompletedTask;
        }

        public Task InvokeErrorEvent(ResponseArgs args)
        {
            OnError?.Invoke(args);
            return Task.CompletedTask;
        }

        public void On(string eventName, EventHandler handler)
        {
            if (!EventHandlers.ContainsKey(eventName))
            {
                EventHandlers.Add(eventName, handler);
            }
        }

        public void Off(string eventName)
        {
            EventHandlers.Remove(eventName);
        }

        private void Emit(string eventName, int packetId, object obj)
        {
            string text = JsonConvert.SerializeObject(obj);
            var builder = new StringBuilder();
            builder
                .Append("42")
                .Append(_namespace)
                .Append(packetId)
                .Append('[')
                .Append('"')
                .Append(eventName)
                .Append('"')
                .Append(',')
                .Append(text)
                .Append(']');

            string message = builder.ToString();
            if (State == SocketState.Connected)
            {
                SendMessage(message);
            }
            else
            {
                throw new InvalidOperationException("Socket connection not ready, emit failure.");
            }
        }

        public void Emit(string eventName, object obj)
        {
            _packetId++;
            Emit(eventName, _packetId, obj);
        }

        public void Emit(string eventName, object obj, EventHandler callback)
        {
            _packetId++;
            Callbacks.Add(_packetId, callback);
            Emit(eventName, _packetId, obj);
        }



    }
}