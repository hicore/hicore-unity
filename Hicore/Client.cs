using System;
using Hicore.KCP;
using Hicore.Logger;
using Hicore.Storage;
using Hicore.Update;


namespace Hicore.Authentications
{
    public class Client : IClient
    {
        private HicoreSocket _socket;
        private Result result;
        private User user;

        private TimeZone curTimeZone;
        private TimeSpan currentOffset;

        public ChildServer ChildServer;

        public UpdateAccount UpdateAccount;
        public Friend Friend;
        public Matchmacker Matchmacker;
        public Communication Communication;
        public DataStorage Storage;
        public StaticDataStorage StaticStorage;
        public MatchController MatchController;
        

        internal static string Token;

        private string authenticateEvent = "authenticate"; // main event

        private string authenticateDeviceIdEvent = "authenticateDeviceId";
        private string authenticateEmailEvent = "authenticateEmail";

        private Action<User, Result> OnAuthenticateDeviceId;
        private Action<User, Result> OnAuthenticateEmail;


        public Client(HicoreSocket socket)
        {
            if (_socket == null)
            {
                this._socket = socket;
                Token = "";

                result = new Result();
                user = new User();

                curTimeZone = TimeZone.CurrentTimeZone;
                currentOffset = curTimeZone.GetUtcOffset(DateTime.Now);


                MatchController = new MatchController(_socket);
                UpdateAccount = new UpdateAccount(_socket);
                Friend = new Friend(_socket);
                Matchmacker = new Matchmacker(_socket);
                Communication = new Communication(_socket);
                Storage = new DataStorage(_socket);
                StaticStorage = new StaticDataStorage(_socket);

                _socket.On(authenticateDeviceIdEvent, res =>
                {
                    JSONNode jsonRes = JSON.Parse(res.Text);

                    if (jsonRes["type"].Value.Equals(result.Success))
                    {
                        user.UserId = jsonRes["userId"].Value;
                        user.Username = jsonRes["username"].Value;
                        user.Token = jsonRes["token"].Value;
                        user.GameInfo = jsonRes["gameInfo"];
                        user.FriendRequest = jsonRes["friendRequest"];
                        user.FriendList = jsonRes["friendList"];
                        user.TotalFriendRequest = jsonRes["totalFriendRequest"];


                        result.Type = jsonRes["type"].Value;
                        result.Message = jsonRes["msg"].Value;
                        result.Code = jsonRes["code"].AsInt;


                        // save token to update account
                        Token = jsonRes["token"].Value;

                        // send user token to Game Server
                        ChildServer?.SendToken();

                        OnAuthenticateDeviceId(user, result);
                    }
                    else
                    {
                        result.Type = jsonRes["type"].Value;
                        result.Message = jsonRes["msg"].Value;
                        result.Code = jsonRes["code"].AsInt;
                        OnAuthenticateDeviceId(user, result);
                    }
                });

                _socket.On(authenticateEmailEvent, res =>
                {
                    JSONNode jsonRes = JSON.Parse(res.Text);

                    if (jsonRes["type"].Value.Equals(result.Success))
                    {
                        user.UserId = jsonRes["userId"].Value;
                        user.Username = jsonRes["username"].Value;
                        user.Token = jsonRes["token"].Value;
                        user.GameInfo = jsonRes["gameInfo"];
                        user.FriendRequest = jsonRes["friendRequest"];
                        user.FriendList = jsonRes["friendList"];
                        user.TotalFriendRequest = jsonRes["totalFriendRequest"];

                        result.Type = jsonRes["type"].Value;
                        result.Message = jsonRes["msg"].Value;
                        result.Code = jsonRes["code"].AsInt;


                        // save token to update account
                        Token = jsonRes["token"].Value;
                        // send user token to Game Server
                        ChildServer?.SendToken();


                        OnAuthenticateEmail(user, result);
                    }
                    else
                    {
                        result.Type = jsonRes["type"].Value;
                        result.Message = jsonRes["msg"].Value;
                        result.Code = jsonRes["code"].AsInt;
                        OnAuthenticateEmail(user, result);
                    }
                });
            }
            else
            {
                throw new ArgumentException("Socket is already in use");
            }
        }


        public void connectChild(string host, int port)
        {
            ChildServer = new ChildServer(host, port);
        }


        public void closeSokcets()
        {
            _socket.CloseAsync();
            ChildServer.Close();
        }

        public void AuthenticateDeviceId(string deviceId, Action<User, Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", AuthenticateType.deviceId.ToString());
            json.Add("deviceId", deviceId);
            json.Add("location", curTimeZone.StandardName.Replace(" Standard Time", ""));
            json.Add("timezone_utc_offset", currentOffset.ToString());

            _socket.Emit(authenticateEvent, json.ToString());

            OnAuthenticateDeviceId = (user, result) => { onResult(user, result); };
        }

        public void AuthenticateEmail(string email, string password, Action<User, Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", AuthenticateType.email.ToString());
            json.Add("email", email.ToLower());
            json.Add("password", password);
            json.Add("location", curTimeZone.StandardName.Replace(" Standard Time", ""));
            json.Add("timezone_utc_offset", currentOffset.ToString());

            _socket.Emit(authenticateEvent, json.ToString());

            OnAuthenticateEmail = (user, result) => { onResult(user, result); };
        }

        public void AuthenticateFacebook(string token, string username, Action<User, Result> onResult)
        {
            throw new NotImplementedException();
        }

        public void AuthenticateGoogle(string token, string username, Action<User, Result> onResult)
        {
            throw new NotImplementedException();
        }


        private enum AuthenticateType
        {
            deviceId,
            email,
            facebook,
            google
        }
    }
}