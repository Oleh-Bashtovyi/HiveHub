namespace HiveHub.Application.Constants;

public static class ProjectMessages
{
    public static readonly string RoomNotFound = "Room not found";
    public static readonly string PlayerNotFound = "Player not found";
    public static readonly string UnknownError = "Unknown error";

    public static class CreateRoom
    {
        public static readonly string UnableToCreateRoom = "Unable to register the room";
    }

    public static class ChangeHost
    {
        public static readonly string CanNotChangeHostMidGame = "Cannot change host during the game.";
        public static readonly string OnlyHostCanChangePermission = "Only host can transfer permissions.";
    }

    public static class ChangeAvatar
    {
        public static readonly string CanNotChangeAvatarMidGame = "Cannot change avatar during the game.";
        public static readonly string AvatarHasBadFormat = "Invalid avatar identifier.";
    }

    public static class JoinRoom
    {
        public static readonly string YouAreAlreadyInRoom = "You are already in the room.";
        public static readonly string CanNotJoinMidGame = "Cannot join mid-game.";
    }

    public static class Kick
    {
        public static readonly string HostCanNotKickItself = "Host cannot kick themselves.";
        public static readonly string OnlyHostCanKickPlayers = "Only host can kick players.";
        public static readonly string CanNotKickPlayersMidGame = "Cannot kick players during the game.";
    }

    public static class Rename
    {
        public static readonly string CanNotChangeGameMidGame = "Cannot change name during the game.";
        public static readonly string PlayerWithThisNameAlreadyExistsInRoom = "Player with this name already exists in the room.";
        public static readonly string PlayerNameMustHaveLength = $"Name must have at least 1 character and be less than {ProjectConstants.PlayerNameMaxLength}.";
    }

    public static class ReturnToLobby
    {
        public static readonly string OnlyHostCanReturnToLobby = "Only host can return to lobby.";
    }

    public static class SendMessage
    {
        public static readonly string BadMessageFormat = $"Message must be between 1 and {ProjectConstants.MessageMaxLength} characters.";
    }

    public static class VoteToStopTimer
    {
        public static readonly string YouHaveAlreadyVoted = "You have already voted.";
        public static readonly string TimerHasAlreadyStoped = "Timer is already stopped.";
        public static readonly string TimeHasPassed = "Time is up.";
        public static readonly string VoteToStopTimerAvailvableOnlyMidGame = "You can stop timer only during game.";
    }

    public static class ToggleReady
    {
        public static readonly string CanNotReadyStatusMidGame = "Cannot change ready status during the game.";
    }

    public static class GetRoomState
    {
        public static readonly string HaveNotFoundPlayerWithThisConnectionIdInRoom = "Player not found in the room by this connection.";
    }

    public static class UpdateSettings
    {
        public static readonly string OnlyHostCanChangeGameSettings = "Only host can change settings.";
        public static readonly string CanNotChangeGameSettingsMidGame = "Cannot change settings during the game.";
    }

    public static class StartGame
    {
        public static readonly string ExceedingMaxPlayerLimit = "Exceeding max players limit";
        public static readonly string GameIsAlreadyStarted = "Game is already in progress. Finish current game first.";
        public static readonly string OnlyHostCanStartGame = "Only host can start the game.";
        public static readonly string NotAllPlayersIsReady = "Not all players are ready. Everyone must press 'Ready'.";
    }

    public static class Accusation
    {
        public static readonly string AlreadyUsed = "Ви вже використали своє право на звинувачення в цьому раунді.";
        public static readonly string VoteInProgress = "Голосування вже триває.";
        public static readonly string TargetNotFound = "Ціль звинувачення не знайдена.";
        public static readonly string CannotAccuseSelf = "Не можна звинуватити себе.";
        public static readonly string CannotAccuseDeadPlayer = "Can not accuse dead player";
        public static readonly string InitiatorNotFound = "Initiator was not found";
        public static readonly string NoActiveVoting = "No active voting";
        public static readonly string UnknownVotingState = "Unknown voting state";

        public static readonly string VoteTypeWasNotSpecified = "Vote type was not specified";
        public static readonly string VotingTargetMismatch = "Voting target mismatch";
        public static readonly string VotaCanBeDoneOnlyDuringSearch = "Звинуватити можливо лише під час пошуку.";
        public static readonly string CanNotAccuseAfterRoundTimeUp = "Неможливо звинуватити, раунд досяг кінця.";

        public static readonly string YouAlreadyVoted = "Ви вже проголосували";
        public static readonly string TargetPlayerIdrequiredForFinalVote = "Target player required for final vote";
    }

    public static class SpyGameJoinRoom
    {
        public static string ExceedingMaxPlayersCount(int maxCount) => 
            $"Max players count reached. Only {maxCount} can join game.";
    }

    public static class SpyGameStartGame
    {
        public static readonly string NoCategoriesWasSet = "No word categories selected for the game.";
        public static readonly string MinimumThreePlayersRequiredToStart = "Not enough players (minimum 3).";
        public static readonly string NoCategoriesWithAtLeastOneWord = "Selected categories do not contain any words.";
        public static readonly string SomeCategoryIsEmpty = "Some category contain no words.";
    }

    public static class SpyGameRevealSpies
    {
        public static readonly string OnlyHostCanRevealSpies = "Only host can reveal spies.";
        public static readonly string TimerMustBeStoppedToRevealSpies = "Timer must be stopped first.";
        public static readonly string RevealSpiesCanBeDoneOnlyMidGame = "Game is not in progress.";
    }

    public static class SpyGameUpdateSettings
    {
        public static readonly string MustBeAtLeastThreePlayers = "Must be at least three players";
        public static string ExceedingMaxPlayersCount(int maxCount) =>
            $"Max players count can not exceed {maxCount}";

        public static readonly string MinSpiesMustBeNonNegative = "Minimum spies count must be 0 or more.";
        public static readonly string MaxSpiesMustBeAtLeastOne = "Maximum spies count must be at least 1.";
        public static readonly string MinSpiesCannotExceedMax = "Minimum spies count cannot be greater than maximum.";

        public static readonly string MaxSpiesCannotBeGraterThan = 
            $"Maximum spies count cannont be grater than {ProjectConstants.SpyGame.MaxPlayersCount}.";

        public static readonly string SomeCustomWordsExceedingLengthLimit = 
            $"Some custom words exceeding max length limits of {ProjectConstants.SpyGame.MaxWordInCategoryLength}";

        public static readonly string MaxCustomCategoriesCountCannotBeGraterThan =
            $"Maximum custom categories count cannont be grater than {ProjectConstants.SpyGame.MaxCustomCategoriesCount}.";

        public static readonly string MaxPlayersCountCanNotBeLessThanAlreadyJoined = "Players max count can not be less than already joined";
        
        public static readonly string GameTimeMustBeInRange =
            $"Game time must be between {ProjectConstants.SpyGame.MinGameDurationMinutes} and {ProjectConstants.SpyGame.MaxGameDurationMinutes} minutes.";
    }

    public static class SpyGameMakeGuess 
    {
        public static readonly string CanBeDoneOnlyMidGame = "Guess can be made only during game";
        public static readonly string OnlySpyCanGuess = "Тільки шпигун може вгадувати.";
        public static readonly string GuessNotAllowedInCurrentPhase = "Вгадувати не можна в поточній фазі гри.";
        public static readonly string OnlyCaughtSpyCanGuessInFinalPhase = "Вгадувати може тільки спійманий шпигун.";
    }
}
