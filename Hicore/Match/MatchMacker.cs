using System;
using System.Collections.Generic;
using Hicore.Authentications;
using Hicore.Logger;
using Hicore.Units;

namespace Hicore
{
    public class Matchmacker
    {
        private HicoreSocket _socket;

        private string matchmakerEvent = "matchmaker";

        private string requestMatchEvent = "requestMatch";
        private string cancelMatchEvent = "cancelMatch";
        private string createPlaymateMatchEvent = "createPlaymateMatch";
        private string destroyPlaymateMatchEvent = "destroyPlaymateMatch";
        private string invitePlaymateEvent = "invitePlaymate";
        private string acceptPlaymateEvent = "acceptPlaymate";
        private string denyPlaymateEvent = "denyPlaymate";
        private string leavePlaymateEvent = "leavePlaymate";


        private Action<Result> OnRequestMatchResult;
        private Action<Result> OnCancelMatchResult;
        private Action<Result> OnCreatePlaymateMatchResult;
        private Action<Result> OnDestroyPlaymateMatchResult;
        private Action<Result> OnInvitePlaymateResult;
        private Action<Result> OnAcceptPlaymateResult;
        private Action<Result> OnDenyPlaymateResult;
        private Action<Result> OnLeavePlaymateResult;

        private string matchmakingResultListenerEvent = "matchmakingResult";
        private string playmateResultListenerEvent = "playmateResult";

        public Action<List<MatchmakerResult> > OnMatchmaking;
        public Action<PlaymateResult> OnPlaymate;


        public Matchmacker(HicoreSocket socket)
        {
            this._socket = socket;

            _socket.On(requestMatchEvent, res => { OnRequestMatchResult(new Result(res.Text)); });

            _socket.On(cancelMatchEvent, res => { OnCancelMatchResult(new Result(res.Text)); });

            _socket.On(createPlaymateMatchEvent, res => { OnCreatePlaymateMatchResult(new Result(res.Text)); });

            _socket.On(destroyPlaymateMatchEvent, res => { OnDestroyPlaymateMatchResult(new Result(res.Text)); });

            _socket.On(invitePlaymateEvent, res => { OnInvitePlaymateResult(new Result(res.Text)); });

            _socket.On(acceptPlaymateEvent, res => { OnAcceptPlaymateResult(new Result(res.Text)); });
            _socket.On(denyPlaymateEvent, res => { OnDenyPlaymateResult(new Result(res.Text)); });

            _socket.On(leavePlaymateEvent, res => { OnLeavePlaymateResult(new Result(res.Text)); });


            // Listener events 
            _socket.On(matchmakingResultListenerEvent, res =>
            {
                JSONNode jsonRes = JSON.Parse(res.Text);

                
                List<MatchmakerResult> opponents = new List<MatchmakerResult>();
                foreach (JSONNode r in jsonRes)
                {
                    MatchmakerResult result = new MatchmakerResult();
                    result.MatchId = r["matchId"].Value;
                    result.PlaymateUserId = r["userId"].Value;
                    result.PlaymateUsername = r["username"].Value;
                    result.PlaymateId = r["playmateId"].Value;
                    result.PlaymateSocketId = r["socketId"].Value;
                    result.PlaymateLevel = r["level"].AsInt;
                    result.PlaymateRank = r["rank"].AsInt;
                    result.Team = r["team"].AsInt;

                    opponents.Add(result);
                }
                
                OnMatchmaking(opponents);
            });


            _socket.On(playmateResultListenerEvent, res =>
            {
                JSONNode jsonRes = JSON.Parse(res.Text);

                PlaymateResult result = new PlaymateResult();

                result.Type = jsonRes["type"].Value;
                result.Code = jsonRes["code"].AsInt;

                if (jsonRes["code"].AsInt == 0) // invite result
                {
                    result.UserId = jsonRes["userId"].Value;
                    result.Username = jsonRes["username"].Value;
                    result.UserSocketId = jsonRes["userSocketId"].Value;
                    result.PlaymateId = jsonRes["playmateId"].Value;
                }

                if (jsonRes["code"].AsInt == 1) // accept result
                {
                    result.UserId = jsonRes["userId"].Value;
                    result.Username = jsonRes["username"].Value;
                }

                if (jsonRes["code"].AsInt == 2) // deny result 
                {
                    result.UserId = jsonRes["userId"].Value;
                    result.Username = jsonRes["username"].Value;
                }

                if (jsonRes["code"].AsInt == 3) // leave
                {
                    result.UserId = jsonRes["userId"].Value;
                    result.Username = jsonRes["username"].Value;
                }

                if (jsonRes["code"].AsInt == 4) // queue
                {
                    result.Message = jsonRes["msg"].Value;
                }

                if (jsonRes["code"].AsInt == 5) // cancel search for game
                {
                    result.Message = jsonRes["msg"].Value;
                }

                if (jsonRes["code"].AsInt == 6) // destroy 
                {
                    result.Message = jsonRes["msg"].Value;
                }


                OnPlaymate(result);
            });
        }


        /// <param name="roomCapacity">the max capacity of that room.</param>
        /// <param name="requestType">request normal game.</param>
        /// <param name="requestTeamNumber">the number of teams </param>
        /// <param name="matchMode">The mode of Match</param>
        /// <param name="onResult">the result of match request.</param>
        public void RequestMatch(int roomCapacity, int requestTeamNumber,string requestType,string matchMode,  Action<Result> onResult)
        {
            //  request match without playmate for normal game 

            if (requestType == "level" || requestType == "rank")
            {
                Result result = new Result();
                result.Type = "error";
                result.Message = "For requesting level or rank match use range parameter";
                onResult(result);
                return;
            }

            Math.DivRem(roomCapacity, requestTeamNumber, out var divideRemaining);
            if (divideRemaining != 0)
            {
                Result result = new Result();
                result.Type = "error";
                result.Message = "Divide the room capacity by the number of teams should be equal to zero";
                onResult(result);
                return;
            }

            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.request.ToString());
            json.Add("roomCapacity", roomCapacity);
            json.Add("requestType", requestType);
            json.Add("requestTeamNumber", requestTeamNumber);
            json.Add("matchMode", matchMode);
            json.Add("token", Client.Token);
            
            _socket.Emit(matchmakerEvent, json.ToString());

            OnRequestMatchResult = (res) => { onResult(res); };
        }

        /// <param name="roomCapacity">the max capacity of that room.</param>
        /// <param name="requestType">the type of query for find game base of user level or rank.</param>
        /// <param name="range">find game between this range.</param>
        /// <param name="requestTeamNumber">the number of teams </param>
        /// <param name="matchMode">The mode of Match</param>
        /// <param name="onResult">the result of match request.</param>
        public void RequestMatch(int roomCapacity, int requestTeamNumber,string requestType, int range, string matchMode, 
            Action<Result> onResult)
        {
            if (requestType == "normal")
            {
                Result result = new Result();
                result.Type = "error";
                result.Message = "You can't use normal request, remove the range parameter or change your request";
                onResult(result);
                return;
            }
            
            Math.DivRem(roomCapacity, requestTeamNumber, out var divideRemaining);
            if (divideRemaining != 0)
            {
                Result result = new Result();
                result.Type = "error";
                result.Message = "Divide the room capacity by the number of teams should be equal to zero";
                onResult(result);
                return;
            }

            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.request.ToString());
            json.Add("roomCapacity", roomCapacity);
            json.Add("requestType", requestType);
            json.Add("range", range);
            json.Add("requestTeamNumber", requestTeamNumber);
            json.Add("matchMode", matchMode);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnRequestMatchResult = (res) => { onResult(res); };
        }

        /// <param name="roomCapacity">the max capacity of that room.</param>
        /// <param name="playmateId">the id of the room which users want play together.</param>
        /// <param name="requestType">request normal game.</param>
        /// <param name="requestTeamNumber">the number of teams </param>
        /// <param name="matchMode">The mode of Match</param>
        /// <param name="onResult">the result of match request.</param>
        public void RequestMatch(int roomCapacity,int requestTeamNumber, string playmateId, string requestType, string matchMode, 
            Action<Result> onResult)
        {
            //  request normal game with playmate

            if (requestType == "level" || requestType == "rank")
            {
                Result result = new Result();
                result.Type = "error";
                result.Message = "For requesting level or rank match use range parameter";
                onResult(result);
                return;
            }
            Math.DivRem(roomCapacity, requestTeamNumber, out var divideRemaining);
            if (divideRemaining != 0)
            {
                Result result = new Result();
                result.Type = "error";
                result.Message = "Divide the room capacity by the number of teams should be equal to zero";
                onResult(result);
                return;
            }

            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.request.ToString());
            json.Add("roomCapacity", roomCapacity);
            json.Add("playmateId", playmateId);
            json.Add("requestType", requestType);
            json.Add("requestTeamNumber", requestTeamNumber);
            json.Add("matchMode", matchMode);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnRequestMatchResult = (res) => { onResult(res); };
        }

        /// <param name="roomCapacity">the max capacity of that room.</param>
        /// <param name="playmateId">the id of the room which users want play together.</param>
        /// <param name="requestType">the type of query for find game base of user level or rank.</param>
        /// <param name="range">find game between this range.</param>
        /// <param name="requestTeamNumber">the number of teams </param>
        /// <param name="matchMode">The mode of Match</param>
        /// <param name="onResult">the result of match request.</param>
        public void RequestMatch(int roomCapacity,  int requestTeamNumber,string playmateId, string requestType, int range,string matchMode, 
            Action<Result> onResult)
        {
            if (requestType == "Normal")
            {
                Result result = new Result();

                result.Type = "error";
                result.Message = "You can't use normal request, remove the range parameter or change your request";

                onResult(result);

                return;
            }
            Math.DivRem(roomCapacity, requestTeamNumber, out var divideRemaining);
            if (divideRemaining != 0)
            {
                Result result = new Result();
                result.Type = "error";
                result.Message = "Divide the room capacity by the number of teams should be equal to zero";
                onResult(result);
                return;
            }

            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.request.ToString());
            json.Add("roomCapacity", roomCapacity);
            json.Add("playmateId", playmateId);
            json.Add("requestType", requestType);
            json.Add("range", range);
            json.Add("requestTeamNumber", requestTeamNumber);
            json.Add("matchMode", matchMode);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnRequestMatchResult = (res) => { onResult(res); };
        }


        public void CancelMatch(Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.cancel.ToString());
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnCancelMatchResult = (res) => { onResult(res); };
        }

        public void CancelMatch(string playmateId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.cancel.ToString());
            json.Add("playmateId", playmateId);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnCancelMatchResult = (res) => { onResult(res); };
        }

        public void CreatePlaymateMatch(Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.createPlaymate.ToString());
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnCreatePlaymateMatchResult = (res) => { onResult(res); };
        }

        public void DestroyPlaymateMatch(string playmateId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.destroyPlaymate.ToString());
            json.Add("playmateId", playmateId);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnDestroyPlaymateMatchResult = (res) => { onResult(res); };
        }

        public void InvitePlaymate(string friendUserId, string playmateId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.invite.ToString());
            json.Add("friendUserId", friendUserId);
            json.Add("playmateId", playmateId);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());


            OnInvitePlaymateResult = (res) => { onResult(res); };
        }

        public void AcceptPlaymate(string friendUserId, string playmateId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.accept.ToString());
            json.Add("friendUserId", friendUserId);
            json.Add("playmateId", playmateId);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnAcceptPlaymateResult = (res) => { onResult(res); };
        }

        public void DenyPlaymate(string friendUserId, string playmateId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.deny.ToString());
            json.Add("friendUserId", friendUserId);
            json.Add("playmateId", playmateId);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnDenyPlaymateResult = (res) => { onResult(res); };
        }

        public void LeavePlaymate(string friendUserId, string playmateId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", MatchTypes.leave.ToString());
            json.Add("friendUserId", friendUserId);
            json.Add("playmateId", playmateId);
            json.Add("token", Client.Token);

            _socket.Emit(matchmakerEvent, json.ToString());

            OnLeavePlaymateResult = (res) => { onResult(res); };
        }


        private enum MatchTypes
        {
            request,
            cancel,
            leave,
            invite,
            accept,
            deny,
            createPlaymate,
            destroyPlaymate
        }
    }


    public class MatchmackerOption
    {
        public static string NormalRequest = "normal";
        public static string LevelRequest = "level";
        public static string RankRequest = "rank";
    }
}