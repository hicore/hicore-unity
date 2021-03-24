
namespace Hicore.Authentications
{

    public class Profile
    {
        private bool timezoneUtcOffset = false;
        private bool location = false;
        private string language = "";
        private string displayName = "";
        private string avatarUrl = "";

        public bool TimezoneUtcOffset { get => timezoneUtcOffset; set => timezoneUtcOffset = value; }
        public bool Location { get => location; set => location = value; }
        public string Language { get => language; set => language = value; }
        public string DisplayName { get => displayName; set => displayName = value; }
        public string AvatarUrl { get => avatarUrl; set => avatarUrl = value; }
    }

}
