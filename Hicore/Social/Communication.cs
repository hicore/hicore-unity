using System;
using Hicore.Authentications;
using Hicore.Logger;

namespace Hicore
{
    public class Communication
    {
        private HicoreSocket _socket;

        private string communicationEvent = "communication"; // main event


        private string messageToIdEvent = "messageToId";
        private string messageToGroupEvent = "messageToGroup";

        private Action<Result> OnMessageToIdResult;
        private Action<Result> OnMessageToGroupResult;



        private string receiveMessageListener = "message";
        public Action<Message> OnMessage;


        public Communication(HicoreSocket socket)
        {
            this._socket = socket;



            _socket.On(messageToIdEvent, res =>
            {
                OnMessageToIdResult(new Result(res.Text));
            });

            _socket.On(messageToGroupEvent, res =>
            {
                OnMessageToGroupResult(new Result(res.Text));
            });


            _socket.On(receiveMessageListener, res =>
            {
                JSONNode jsonRes = JSON.Parse(res.Text);

                Message msg = new Message();

                msg.Type = jsonRes["type"].Value;
                msg.Text = jsonRes["text"].Value;
                msg.SenderId = jsonRes["senderId"].Value;
                msg.SenderUsername = jsonRes["senderUsername"].Value;
                msg.SenderSocketId = jsonRes["senderSocketId"].Value;

                OnMessage(msg);

            });

        }



        public void SendMessageToId(string receiverUserId, string text, Action<Result> onResult)
        {

            JSONObject json = new JSONObject();
            json.Add("type", CommunicationType.toId.ToString());
            json.Add("receiverUserId", receiverUserId);
            json.Add("text", text);
            json.Add("token", Client.Token);

            _socket.Emit(communicationEvent, json.ToString());

            OnMessageToIdResult = (res) => { onResult(res); };

        }

        public void SendMessageToGroup(string playmateId, string text, Action<Result> onResult)
        {

            JSONObject json = new JSONObject();
            json.Add("type", CommunicationType.toGroup.ToString());
            json.Add("playmateId", playmateId);
            json.Add("text", text);
            json.Add("token", Client.Token);

            _socket.Emit(communicationEvent, json.ToString());

            OnMessageToGroupResult = (res) => { onResult(res); };

        }

        private enum CommunicationType
        {
            toId,
            toGroup
        }

    }
}
