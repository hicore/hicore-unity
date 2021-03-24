

namespace Hicore.Units
{
    public class PlaymateResult
    {

        string message;
        string userId;
        string username;
        string userSocketId;
        string playmateId;
        string type;
        int code;


        public string Message { get => message; set => message = value; }
        public string UserId { get => userId; set => userId = value; }
        public string Username { get => username; set => username = value; }
        public string UserSocketId { get => userSocketId; set => userSocketId = value; }
        public string PlaymateId { get => playmateId; set => playmateId = value; }
        public string Type { get => type; set => type = value; }
        public int Code { get => code; set => code = value; }
      
    }
}
