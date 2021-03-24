

namespace Hicore.Units
{
   public class MatchmakerResult
    {
        
        private string matchId;
        private string playmateUserId;
        private string playmateUsername;
        private string playmateId;
        private string playmateSocketId;
        private int playmateLevel;
        private int playmateRank;
        private int team;

       
        public string MatchId { get => matchId; set => matchId = value; }
        public string PlaymateUserId { get => playmateUserId; set => playmateUserId = value; }
        public string PlaymateUsername { get => playmateUsername; set => playmateUsername = value; }
        public string PlaymateId { get => playmateId; set => playmateId = value; }
        public string PlaymateSocketId { get => playmateSocketId; set => playmateSocketId = value; }
        public int PlaymateLevel { get => playmateLevel; set => playmateLevel = value; }
        public int PlaymateRank { get => playmateRank; set => playmateRank = value; }
        public int Team { get => team; set => team = value; }
    }
}
