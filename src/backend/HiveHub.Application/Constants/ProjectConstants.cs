namespace HiveHub.Application.Constants;

public static class ProjectConstants
{
    public static readonly short PlayerNameMaxLength = 50;
    public static readonly short MessageMaxLength = 200;
    public static readonly short MessagesMaxCount = 60;
    public static readonly short RoomCodeLength = 6;
    public static readonly short PlayerIdLength = 8;
    public static readonly short PlayerDisconnectTimeoutSeconds = 30;
    public static readonly string DefaultAvatarId = "default";

    public static class SpyGame
    {
        public static readonly short MaxPlayersCount = 8;
        public static readonly short MaxGameDurationMinutes = 30;
        public static readonly short MinGameDurationMinutes = 1;
        public static readonly short MaxCustomCategoriesCount = 10;
        public static readonly short AccusationVoteDurationSeconds = 30;
        public static readonly short FinalVoteDurationSeconds = 60;
    }
}
