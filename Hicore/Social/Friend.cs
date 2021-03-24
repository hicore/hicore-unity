using System;
using Hicore.Authentications;
using Hicore.Logger;

namespace Hicore
{
    public class Friend
    {
        private HicoreSocket _socket;

        private string friendEvent = "friend";


        private string searchFriendEvent = "searchFriend";
        private string requestFriendEvent = "requestFriend";
        private string acceptFriendEvent = "acceptFriend";
        private string removeFriendEvent = "removeFriend";
        private string rejectFriendEvent = "rejectFriend";
        

        private Action<Result> OnSearchFriendResult;
        private Action<Result> OnRequestFriendResult;
        private Action<Result> OnAcceptFriendResult;
        private Action<Result> OnRemoveFriendResult;
        private Action<Result> OnRejectFriendResult;

        public Friend(HicoreSocket socket)
        {
            this._socket = socket;


            _socket.On(searchFriendEvent, res =>
            {

                OnSearchFriendResult(new Result(res.Text));

            });

            _socket.On(requestFriendEvent, res =>
            {

                OnRequestFriendResult(new Result(res.Text));

            });

            _socket.On(acceptFriendEvent, res =>
            {
                OnAcceptFriendResult(new Result(res.Text));
            });

            _socket.On(rejectFriendEvent, res =>
            {
                OnRejectFriendResult(new Result(res.Text));
            });

            _socket.On(removeFriendEvent, res =>
            {
                OnRemoveFriendResult(new Result(res.Text));

            });

        }
        public void SearchFriend(string searchUsername, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", FriendTypes.search.ToString());
            json.Add("searchUsername", searchUsername);
            json.Add("token", Client.Token);

            _socket.Emit(friendEvent, json.ToString());

            OnSearchFriendResult = (res) => { onResult(res); };



        }

        public void RequestFriend(string receiverUserId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", FriendTypes.request.ToString());
            json.Add("receiverUserId", receiverUserId);
            json.Add("token", Client.Token);

            _socket.Emit(friendEvent, json.ToString());

            OnRequestFriendResult = (res) => { onResult(res); };


        }
        public void AcceptFriend(string applicantUserId, string applicantUsername, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", FriendTypes.accept.ToString());
            json.Add("applicantUserId", applicantUserId);
            json.Add("applicantUsername", applicantUsername);
            json.Add("token", Client.Token);

            _socket.Emit(friendEvent, json.ToString());

            OnAcceptFriendResult = (res) => { onResult(res); };

        }

        public void RejectFriend(string rejectId, Action<Result> onResult) 
        {
            JSONObject json = new JSONObject();
            json.Add("type", FriendTypes.reject.ToString());
            json.Add("rejectId", rejectId);
            json.Add("token", Client.Token);

            _socket.Emit(friendEvent, json.ToString());

            OnRejectFriendResult = (res) => { onResult(res); };
        }

        public void RemoveFriend(string friendId , Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", FriendTypes.remove.ToString());
            json.Add("friendId",friendId);
            json.Add("token", Client.Token);

            _socket.Emit(friendEvent, json.ToString());

            OnRemoveFriendResult = (res) => { onResult(res); };

        }

    


        private enum FriendTypes
        {
            search,
            request,
            accept,
            reject,
            remove
        }



    }
}
