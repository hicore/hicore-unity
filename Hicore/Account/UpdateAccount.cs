using System;
using Hicore.Authentications;
using Hicore.Logger;

namespace Hicore.Update
{
    public class UpdateAccount : IUpdateAccount
    {


        private HicoreSocket _socket;


        private TimeZone curTimeZone;
        private TimeSpan currentOffset;

        private string updateEvent = "update"; // main event

        private string updateUserProfileEvent = "updateUserProfile";
        private string updateUserPasswordEvent = "updateUserPassword";
        private string updateUserUsernameEvent = "updateUserUsername";
        private string updateUserEmailEvent = "updateUserEmail";
        private string updateGameInfoEvent = "updateGameInfo";
        private string updateUserXpProgressEvent = "updateUserXpProgress";
        private string updateUserSkillProgressEvent = "updateUserSkillProgress";

        private Action<Result> OnUpdateProfileResult;
        private Action<Result> OnUpdateUsernameResult;
        private Action<Result> OnUpdateEmailResult;
        private Action<Result> OnUpdatePasswordResult;
        private Action<Result> OnUpdateGameInfoResult;
        private Action<Result> OnUpdateUserXpProgressResult;
        private Action<Result> OnUpdateUserSkillProgressResult;
        public UpdateAccount(HicoreSocket socket)
        {
            this._socket = socket;


            curTimeZone = TimeZone.CurrentTimeZone;
            currentOffset = curTimeZone.GetUtcOffset(DateTime.Now);

            _socket.On(updateUserProfileEvent, res =>
            {

                OnUpdateProfileResult(new Result(res.Text));

            });

            _socket.On(updateUserUsernameEvent, res =>
            {

                JSONNode jsonRes = JSON.Parse(res.Text);

                Result result = new Result();

                result.Type = jsonRes["type"].Value;
                result.Message = jsonRes["msg"].Value;
                result.Code = jsonRes["code"].AsInt;

                if (jsonRes["data"] != null)
                {
                    //  renew Token
                    Client.Token = jsonRes["data"]["token"].Value;
                }

                OnUpdateUsernameResult(result);

            });

            _socket.On(updateUserEmailEvent, res =>
             {
                 OnUpdateEmailResult(new Result(res.Text));
             });

            _socket.On(updateUserPasswordEvent, res =>
            {
                OnUpdatePasswordResult(new Result(res.Text));
            });

            _socket.On(updateGameInfoEvent, res =>
            {
                OnUpdateGameInfoResult(new Result(res.Text));
            });

            _socket.On(updateUserXpProgressEvent, res =>
            {
                OnUpdateUserXpProgressResult(new Result(res.Text));
            });
            
            _socket.On(updateUserSkillProgressEvent, res =>
            {
                OnUpdateUserSkillProgressResult(new Result(res.Text));
            });

        }

        public void Password(string oldPassword, string newPassword, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.password.ToString());
            json.Add("oldPassword", oldPassword);
            json.Add("newPassword", newPassword);
            json.Add("token", Client.Token);

            _socket.Emit(updateEvent, json.ToString());

            OnUpdatePasswordResult = (res) => { onResult(res); };
        }

        public void Username(string newUsername, Action<Result> onResult)
        {

            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.username.ToString());
            json.Add("username", newUsername.ToLower());
            json.Add("token", Client.Token);

            _socket.Emit(updateEvent, json.ToString());

            OnUpdateUsernameResult = (res) => { onResult(res); };
        }

        public void Email(string newEmail, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.email.ToString());
            json.Add("email", newEmail.ToLower());
            json.Add("token", Client.Token);

            _socket.Emit(updateEvent, json.ToString());

            OnUpdateEmailResult = (res) => { onResult(res); };
        }




        public void UserProfile(Profile profile, Action<Result> onResult)
        {

            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.profile.ToString());
            json.Add("token", Client.Token);

            if (profile.Language != "")
            {
                json.Add("lang", profile.Language);
            }
            if (profile.Location == true)
            {
                string newLocation = curTimeZone.StandardName.Replace(" Standard Time", "");
                json.Add("location", newLocation);
            }
            if (profile.TimezoneUtcOffset == true)
            {
                string timezone_utc_offset = currentOffset.ToString();
                json.Add("timezone_utc_offset", timezone_utc_offset);
            }
            if (profile.AvatarUrl != "")
            {
                json.Add("avatar_url", profile.AvatarUrl);
            }
            if (profile.DisplayName != "")
            {
                json.Add("display_name", profile.DisplayName);
            }

            _socket.Emit(updateEvent, json.ToString());

            OnUpdateProfileResult = (res) =>
            {
                onResult(res);
                profile = null;
            };
        }

        public void GameInfo(bool playerWin, Action<Result> onResult)
        {

            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.gameInfo.ToString());
            json.Add("playerWin", playerWin);
            json.Add("token", Client.Token);

            _socket.Emit(updateEvent, json.ToString());

            OnUpdateGameInfoResult = (res) => { onResult(res); };

        }

        public void UpdateLevelXp(int xp, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.progress.ToString());
            json.Add("xp", xp);
            json.Add("token", Client.Token);

            _socket.Emit(updateEvent, json.ToString());
            
            OnUpdateUserXpProgressResult = (res) => { onResult(res); };
        }
        
        public void UpdateRankSkill(int skill, Action<Result> onResult)
        {
            JSONObject json = new JSONObject();
            json.Add("type", UpdateTypes.progress.ToString());
            json.Add("skill", skill);
            json.Add("token", Client.Token);

            _socket.Emit(updateEvent, json.ToString());
            
            OnUpdateUserSkillProgressResult = (res) => { onResult(res); };
        }

        private enum UpdateTypes
        {
            profile,
            password,
            username,
            email,
            gameInfo,
            progress,

        }

    }


}
