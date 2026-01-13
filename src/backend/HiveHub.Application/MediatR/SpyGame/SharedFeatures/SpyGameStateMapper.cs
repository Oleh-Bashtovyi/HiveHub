using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Domain.Models;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class SpyGameStateMapper
{
    public static SpyRoomStateDto GetRoomStateForPlayer(SpyRoom room, string playerId)
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

            return new SpyPlayerDto(
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

        var settingsDto = new SpyRoomGameSettingsDto(
            TimerMinutes: room.GameSettings.TimerMinutes,
            MaxSpiesCount: room.GameSettings.MaxSpiesCount,
            MinSpiesCount: room.GameSettings.MinSpiesCount,
            SpiesKnowEachOther: room.GameSettings.SpiesKnowEachOther,
            ShowCategoryToSpy: room.GameSettings.ShowCategoryToSpy,
            CustomCategories: room.GameSettings.CustomCategories.Select(c => new WordsCategoryDto(c.Name, c.Words)).ToList()
        );

        // 3. Map GameState (Only if active or ended)
        GameStateDto? gameState = null;

        if (room.Status == RoomStatus.InGame || room.Status == RoomStatus.Ended)
        {
            string? secretWord;
            string? category;

            // Логіка видимості слова та категорії
            if (room.Status == RoomStatus.Ended)
            {
                secretWord = room.CurrentSecretWord;
                category = room.CurrentCategory;
            }
            else
            {
                // Шпигун не бачить слово
                secretWord = isTargetPlayerSpy ? null : room.CurrentSecretWord;

                // Шпигун бачить категорію тільки якщо дозволено налаштуваннями
                var canSeeCategory = !isTargetPlayerSpy || room.GameSettings.ShowCategoryToSpy;
                category = canSeeCategory ? room.CurrentCategory : null;
            }

            var activeVotesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);

            VotingStateDto? votingDto = null;

            if (room.ActiveVoting != null)
            {
                var activePlayersCount = room.Players.Count(p => p.IsConnected);
                var votesRequired = (int)Math.Floor(activePlayersCount / 2.0) + 1;

                if (room.ActiveVoting is AccusationVotingState accState)
                {
                    var accusedName = room.Players.FirstOrDefault(p => p.IdInRoom == accState.TargetId)?.Name;

                    votingDto = new VotingStateDto(
                        Type: VotingType.Accusation,
                        AccusedPlayerId: accState.TargetId,
                        AccusedPlayerName: accusedName,
                        TargetVoting: accState.Votes,
                        AgainstVoting: null,
                        VotesReqired: votesRequired,
                        StartedAt: accState.VotingStartedAt,
                        EndsAt: accState.VotingEndsAt
                    );
                }
                else if (room.ActiveVoting is GeneralVotingState generalState)
                {
                    votingDto = new VotingStateDto(
                        Type: VotingType.Final,
                        AccusedPlayerId: null,
                        AccusedPlayerName: null,
                        TargetVoting: null,
                        AgainstVoting: generalState.Votes!,
                        VotesReqired: votesRequired,
                        StartedAt: generalState.VotingStartedAt,
                        EndsAt: generalState.VotingEndsAt
                    );
                }
            }

            string? caughtSpyName = null;
            if (!string.IsNullOrEmpty(room.CaughtSpyId))
            {
                caughtSpyName = room.Players.FirstOrDefault(x => x.IdInRoom == room.CaughtSpyId)?.Name;
            }

            gameState = new GameStateDto(
                CurrentSecretWord: secretWord,
                Category: category,
                GameStartTime: room.TimerState.GameStartTime ?? DateTime.UtcNow,
                GameEndTime: room.TimerState.PlannedGameEndTime,
                IsTimerStopped: room.TimerState.IsTimerStopped,
                TimerStoppedAt: room.TimerState.TimerStoppedAt,
                TimerVotesCount: activeVotesCount,
                Phase: room.CurrentPhase,
                ActiveVoting: votingDto,
                CaughtSpyId: room.CaughtSpyId,
                CaughtSpyName: caughtSpyName
            );
        }

        var messages = room.ChatMessages
            .TakeLast(50)
            .Select(m => new ChatMessageDto(m.PlayerId, m.PlayerName, m.Message, m.Timestamp))
            .ToList();

        var stateDto = new SpyRoomStateDto(
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
