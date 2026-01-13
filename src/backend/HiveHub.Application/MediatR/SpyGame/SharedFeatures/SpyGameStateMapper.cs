using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Domain.Models;

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

            if (room.Status == RoomStatus.Ended)
            {
                showIsSpy = true;
            }
            else if (room.Status == RoomStatus.InGame)
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
            MaxSpiesCount: room.GameSettings.MaxSpiesCount,
            MinSpiesCount: room.GameSettings.MinSpiesCount,
            SpiesKnowEachOther: room.GameSettings.SpiesKnowEachOther,
            ShowCategoryToSpy: room.GameSettings.ShowCategoryToSpy,
            CustomCategories: room.GameSettings.Categories.Select(c => new WordsCategoryDto(c.Name, c.Words)).ToList()
        );

        GameStateDto? gameState = null;

        if (room.Status == RoomStatus.InGame || room.Status == RoomStatus.Ended)
        {
            string? secretWord;
            string? category;

            if (room.Status == RoomStatus.Ended)
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
                TimerVotesCount: activeVotesCount
            );
        }

        var messages = room.ChatMessages
            .TakeLast(50)
            .Select(m => new ChatMessageDto(m.PlayerId, m.PlayerName, m.Message, m.Timestamp))
            .ToList();

        var stateDto = new RoomStateDto(
            RoomCode: room.RoomCode,
            Status: room.Status,
            Players: playersDto,
            Settings: settingsDto,
            GameState: gameState,
            Messages: messages,
            Version: room.StateVersion
        );

        return stateDto;
    }
}
