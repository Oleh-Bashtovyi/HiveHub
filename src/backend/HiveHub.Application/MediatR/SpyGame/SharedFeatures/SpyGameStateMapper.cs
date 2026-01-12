using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Domain;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class SpyGameStateMapper
{
    public static RoomStateDto GetRoomStateForPlayer(SpyRoom room, string playerId)
    {
        var targetPlayer = room.Players.FirstOrDefault(x => x.IdInRoom == playerId);

        if (targetPlayer == null)
        {
            throw new Exception(ProjectMessages.RoomNotFound);
        }

        var isTargetPlayerSpy = targetPlayer.PlayerState.IsSpy;
        var isSpiesKnowEachOther = room.GameSettings.SpiesKnowEachOther;

        var playersDto = room.Players.Select(p =>
        {
            var isTargetPlayer = p.IdInRoom == playerId;

            var showIsSpy = false;

            if (room.State == RoomState.Ended)
            {
                showIsSpy = true;
            }
            else if (room.State == RoomState.InGame)
            {
                showIsSpy = isTargetPlayer || (isTargetPlayerSpy && isSpiesKnowEachOther);
            }

            return new PlayerDto(
                Id: p.IdInRoom,
                Name: p.Name,
                IsHost: p.IsHost,
                IsReady: p.IsReady,
                AvatarId: p.AvatarId,
                IsConnected: p.IsConnected,
                IsVotedToStopTimer: p.PlayerState.VotedToStopTimer,
                IsSpy: showIsSpy ? p.PlayerState.IsSpy : null
            );
        }).ToList();

        var settingsDto = new RoomGameSettingsDto(
            TimerMinutes: room.GameSettings.TimerMinutes,
            SpiesCount: room.GameSettings.SpiesCount,
            SpiesKnowEachOther: room.GameSettings.SpiesKnowEachOther,
            ShowCategoryToSpy: room.GameSettings.ShowCategoryToSpy,
            WordsCategories: room.GameSettings.Categories.Select(c => new WordsCategoryDto(c.Name, c.Words)).ToList()
        );

        GameStateDto? gameState = null;

        if (room.State == RoomState.InGame || room.State == RoomState.Ended)
        {
            string? secretWord;
            string? category;

            if (room.State == RoomState.Ended)
            {
                secretWord = room.CurrentSecretWord;
                category = room.CurrentCategory;
            }
            else
            {
                secretWord = isTargetPlayerSpy ? null : room.CurrentSecretWord;
                var canSeeCategory = !isTargetPlayerSpy || room.GameSettings.ShowCategoryToSpy;
                category = canSeeCategory ? room.CurrentCategory : null;
            }

            var activeVotesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);

            gameState = new GameStateDto(
                CurrentSecretWord: secretWord,
                Category: category,
                GameStartTime: room.TimerState.GameStartTime ?? DateTime.UtcNow,
                GameEndTime: room.TimerState.PlannedGameEndTime,
                IsTimerStopped: room.TimerState.IsTimerStopped,
                TimerStoppedAt: room.TimerState.TimerStoppedAt,
                TimerVotesCount: activeVotesCount,
                RecentMessages: room.ChatMessages
                    .TakeLast(50)
                    .Select(m => new ChatMessageDto(m.PlayerId, m.PlayerName, m.Message, m.Timestamp))
                    .ToList()
            );
        }

        var stateDto = new RoomStateDto(
            RoomCode: room.RoomCode,
            State: room.State,
            Players: playersDto,
            Settings: settingsDto,
            GameState: gameState,
            Version: room.StateVersion
        );

        return stateDto;
    }
}
