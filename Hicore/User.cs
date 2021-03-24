
namespace Hicore
{
    public class User
    {
        private string userId;
        private string username;
        private string deviceId;
        private string password;
        private string email;
        private string token;
        private JSONNode gameInfo;
        private JSONNode friendList;
        private JSONNode friendRequest;
        private int totalFriendRequest;

        public string UserId { get => userId; set => userId = value; }
        public string Username { get => username; set => username = value; }
        public string DeviceId { get => deviceId; set => deviceId = value; }
        public string Password { get => password; set => password = value; }
        public string Email { get => email; set => email = value; }
        public string Token { get => token; set => token = value; }
        public JSONNode GameInfo { get => gameInfo; set => gameInfo = value; }
        public JSONNode FriendList { get => friendList; set => friendList = value; }
        public JSONNode FriendRequest { get => friendRequest; set => friendRequest = value; }
        public int TotalFriendRequest { get => totalFriendRequest; set => totalFriendRequest = value; }

    }
}
