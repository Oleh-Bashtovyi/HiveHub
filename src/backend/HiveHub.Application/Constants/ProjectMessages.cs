using Microsoft.Win32;
using System.Runtime.InteropServices;

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
        public static readonly string CanNotChangeHostMidGame = "Не можна змінювати хоста під час гри.";
        public static readonly string OnlyHostCanChangePermission = "Тільки хост може передавати права.";
    }

    public static class ChangeAvatar
    {
        public static readonly string CanNotChangeAvatarMidGame = "Не можна змінювати аватар під час гри.";
        public static readonly string AvatarHasBadFormat = "Некоректний ідентифікатор аватара.";

    }

    public static class JoinRoom
    {
        public static readonly string YouAreAlreadyInRoom = "You are already in room";
        public static readonly string CanNotJoinMidGame = "Can not join mid game";
        public static readonly string SpyGameExceedingMaxPlayersCount =
            $"Max players count reached. Only {ProjectConstants.SpyGameMaxPlayersCount} can join game simulteniously";

    }

    public static class Kick
    {
        public static readonly string HostCanNotKickItself = "Хост не може вигнати сам себе.";
        public static readonly string OnlyHostCanKickPlayers = "Тільки хост може виганяти гравців.";
        public static readonly string CanNotKickPlayersMidGame = "Не можна виганяти гравців під час гри.";
    }

    public static class Rename
    {
        public static readonly string CanNotChangeGameMidGame = "Не можна змінювати ім'я під час гри.";
        public static readonly string PlayerWithThisNameAlreadyExistsInRoom = "Гравець з таким ім'ям вже існує в кімнаті.";
        public static readonly string PlayerNameMustHaveLength = $"Name must have at least 1 character and be less than {ProjectConstants.PlayerNameMaxLength}";
    }

    public static class ReturnToLobby
    {
        public static readonly string OnlyHostCanReturnToLobby = "Тільки хост може повернутися в лобі.";
    }

    public static class SendMessage
    {
        public static readonly string ChatAvailableOnlyMidGame = "Чат доступний тільки під час гри.";
        public static readonly string BadMessageFormat = $"Повідомлення повинно бути від 1 до {ProjectConstants.MessageMaxLength} символів.";
    }

    public static class VoteToStopTimer
    {
        public static readonly string YouHaveAlreadyVoted = "Ви вже проголосували.";
        public static readonly string TimerHasAlreadyStoped = "Таймер вже зупинено.";
        public static readonly string TimeHasPassed = "Час вийшов.";
        public static readonly string VoteToStopTimerAvailvableOnlyMidGame = "You can stop timer only during game";
    }

    public static class ToggleReady
    {
        public static readonly string CanNotReadyStatusMidGame = "Не можна змінювати статус готовності під час гри.";
    }

    public static class GetRoomState
    {
        public static readonly string HaveNotFoundPlayerWithThisConnectionIdInRoom = "Гравця не знайдено в кімнаті за цим з'єднанням.";
    }

    public static class UpdateSettings
    {
        public static readonly string OnlyHostCanChangeGameSettings = "Тільки хост може змінювати налаштування.";
        public static readonly string CanNotChangeGameSettingsMidGame = "Не можна змінювати налаштування під час гри.";
    }

    public static class StartGame
    {
        public static readonly string GameIsAlreadyStarted = "Гра вже йде. Спочатку завершіть поточну.";
        public static readonly string OnlyHostCanStartGame = "Тільки хост може почати гру.";
        public static readonly string NotAllPlayersIsReady = "Не всі гравці готові. Всі повинні натиснути 'Готовий'.";
    }

    public static class SpyGameStartGame
    {
        public static readonly string NoCategoriesWasSet = "Немає категорій слів для гри.";
        public static readonly string MinimumThreePlayersRequiredToStart = "Недостатньо гравців (мінімум 3).";
        public static readonly string NoCategoriesWithAtLeastOneWord = "Немає категорій що містять хоча б одне слово.";
    }

    public static class SpyGameRevealSpies
    {
        public static readonly string OnlyHostCanRevealSpies = "Тільки хост може розкрити шпигунів.";
        public static readonly string TimerMustBeStoppedToRevealSpies = "Спочатку потрібно зупинити таймер.";
        public static readonly string RevealSpiesCanBeDoneOnlyMidGame = "Гра не йде.";
    }

    public static class SpyGameUpdateSettings
    {
        public static readonly string SpiesCountMustBeMinimumOne = "Кількість шпигунів повинна бути мінімум 1.";
        public static readonly string GameTimeMustBeInRange = 
            $"Час гри повинен бути від {ProjectConstants.SpyGameMinGameDurationMinutes} до {ProjectConstants.SpyGameMaxGameDurationMinutes} хвилин.";
    }
}
