using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hicore.Authentications;

namespace Hicore.KCP
{
    public class ChildServer
    {
        private AsyncRudpClient reliableUDP;

        public event Action OnConnected;
        public event Action<String> OnClosed;
        public event Action<String> OnError;

        public Action<String> OnReceivePacket;


        public ChildServer(string host, int port)
        {
            //  Setup Reliable UDP Server
            reliableUDP = new AsyncRudpClient {AckNoDelay = true, WriteDelay = false};

            reliableUDP.StartClient(host, port);

            // reliableUDP.OnConnected = () => { };

            reliableUDP.ReceiveData = (data) => { OnReceivePacket(data); };
        }


        public void SendToken() // TODO: use just in side dll changed to protected
        {
            JSONObject json = new JSONObject();
            json.Add("event", "registerUser");
            json.Add("tokenData", Client.Token);
            var dataToSend = Encoding.UTF8.GetBytes(json.ToString());
            var sent = reliableUDP.Send(dataToSend, 0, dataToSend.Length);
            if (sent < 0)
            {
                Console.WriteLine("Write message failed.");
            }
        }

        /// <summary>
        /// With this function, we can save all game states like last user position,
        /// last weapons, last set , kills user have, is user alive or not  and so on.
        /// We can even save environments states.
        /// by calling this function data is overwrite every time by new data for each id
        /// </summary>
        /// <param name="matchId">The match id of the game .</param>
        /// <param name="id">The id of user or id of environments </param>
        /// <param name="matchData">The data of states in json format</param>
        public void SaveMatchData(string matchId, string id, JSONNode matchData)
        {
            JSONObject json = new JSONObject();
            json.Add("event", "saveMatchData");
            json.Add("tokenData", Client.Token);

            JSONObject dataJson = new JSONObject();
            dataJson.Add("matchId", matchId);
            dataJson.Add("id", id);
            dataJson.Add("matchData", matchData);

            json.Add("data", dataJson.ToString());

            var dataToSend = Encoding.UTF8.GetBytes(json.ToString());
            var sent = reliableUDP.Send(dataToSend, 0, dataToSend.Length);
            if (sent < 0)
            {
                Console.WriteLine("Write message failed.");
            }
        }

        void PingPong()
        {
        }

        public void Close()
        {
            reliableUDP.CloseAsync();
        }

        public void ChangeGameServer(string host, int port)
        {
        }

        public void ReConnectToServer()
        {
            // agahr game server off shod ye bard dg bayad connect beshe ke address to list garar begire
        }

        public void SendReliableData(JSONObject data)
        {
            JSONObject json = new JSONObject();
            json.Add("event", "broadcast");
            json.Add("data", data);
            var dataToSend = Encoding.UTF8.GetBytes(json.ToString());
            var sent = reliableUDP.Send(dataToSend, 0, dataToSend.Length);
            if (sent < 0)
            {
                Console.WriteLine("Write message failed.");
            }
        }
        
        public void SendMatchState(string matchId, string userId, int team, dynamic action )
        {
            JSONObject json = new JSONObject();
            json.Add("event", "matchState");
            json.Add("tokenData", Client.Token);

            JSONObject dataJson = new JSONObject();
            dataJson.Add("matchId", matchId);
            dataJson.Add("userId", userId);
            dataJson.Add("team", team);
            dataJson.Add("action", action);

            json.Add("data", dataJson.ToString());

            var dataToSend = Encoding.UTF8.GetBytes(json.ToString());
            var sent = reliableUDP.Send(dataToSend, 0, dataToSend.Length);
            if (sent < 0)
            {
                Console.WriteLine("Write message failed.");
            }
        }
    }
}