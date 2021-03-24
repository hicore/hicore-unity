
namespace Hicore
{
    public class Message
    {
        string type;
        string text;
        string senderId;
        string senderUsername;
        string senderSocketId;

        public static string ToId = "toId";
        public static string ToGroup = "toGroup";

        public string Type { get => type; set => type = value; }
        public string Text { get => text; set => text = value; }
        public string SenderId { get => senderId; set => senderId = value; }
        public string SenderUsername { get => senderUsername; set => senderUsername = value; }
        public string SenderSocketId { get => senderSocketId; set => senderSocketId = value; }

    }
}
