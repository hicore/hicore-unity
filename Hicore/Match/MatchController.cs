using System;
using Hicore.Authentications;
using Hicore.Logger;
using Hicore.Units;

namespace Hicore
{
    public class MatchController
    {
        private HicoreSocket _socket;

        private string matchControllerEvent = "matchController"; // main event

        private string isMatchInProgressEvent = "isMatchInProgress";
        private string joinToMatchEvent = "joinToMatch";
        private string leaveMatchEvent = "leaveMatch";

        // listeners events

        private string endOfMatchEvent = "endOfMatch";


        // callbacks
        private Action<Result> OnIsMatchInProgressResult;
        private Action<Result> OnJoinToMatchResult;
        private Action<Result> OnLeaveMatch;


        public Action<FinalResult> OnEnd;

        public MatchController(HicoreSocket socket)
        {
            this._socket = socket;


            _socket.On(isMatchInProgressEvent, res => { OnIsMatchInProgressResult(new Result(res.Text)); });

            _socket.On(joinToMatchEvent, res => { OnJoinToMatchResult(new Result(res.Text)); });

            _socket.On(leaveMatchEvent, res => { OnLeaveMatch(new Result(res.Text)); });

            _socket.On(endOfMatchEvent, res =>
            {
                JSONNode jsonRes = JSON.Parse(res.Text);
                FinalResult result = new FinalResult();

                result.Message = jsonRes["msg"].Value;
                result.Data = jsonRes["data"];

                OnEnd(result);
            });
        }

        /// <summary>
        /// If you want to know the game is in progress or not 
        /// then join that user to the game again 
        /// </summary>
        /// <param name="matchId">the id of the match .</param>
        /// <param name="second">the minimum time left from that game.</param>
        /// <param name="onResult"></param>
        /// <returns>true , false</returns>
        public void IsMatchInProgressStatus(string matchId,  Action<Result> onResult)
        {
            
            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.isMatchInProgress.ToString());
            json.Add("matchId", matchId);
            json.Add("token", Client.Token);

            _socket.Emit(matchControllerEvent, json.ToString());

            OnIsMatchInProgressResult = onResult;
        }

        /// <summary>
        /// User can join to the previous game if it exists
        /// </summary>
        /// <param name="matchId">the id of the match .</param>
        /// <param name="onResult"></param>
        /// <returns>the whole data of that game</returns>
        public void JoinToMatch(string matchId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.joinToMatch.ToString());
            json.Add("matchId", matchId);
            json.Add("token", Client.Token);

            _socket.Emit(matchControllerEvent, json.ToString());

            OnJoinToMatchResult = onResult;
        }

        /// <summary>
        /// User can leave from the game in progress if it exists
        /// </summary>
        /// <param name="matchId">the id of the match .</param>
        /// <param name="onResult"></param>
        /// <returns>success</returns>
        public void LeaveMatch(string matchId, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.leaveMatch.ToString());
            json.Add("matchId", matchId);
            json.Add("token", Client.Token);

            _socket.Emit(matchControllerEvent, json.ToString());

            OnLeaveMatch = onResult;
        }

        private enum UpdateTypes
        {
            isMatchInProgress,
            joinToMatch,
            leaveMatch,
        }
    }
}