using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Domain.Models.Shared;
using HiveHub.Domain.Models.SpyGame;

namespace HiveHub.Application.MediatR.SpyGame.SharedFeatures;

public static class SpyGameStateMapper
{
    public static SpyRoomStateDto GetRoomStateForPlayer(SpyRoom room, string playerId)
    {
        if (!room.TryGetPlayerByIdInRoom(playerId, out var targetPlayer))
        {
            throw new Exception(ProjectMessages.RoomNotFound);
        }

        // Map players
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
                HasUsedAccusation: p.PlayerState.HasUsedAccusation,
                IsVotedToStopTimer: p.PlayerState.VotedToStopTimer,
                IsDead: p.PlayerState.IsDead,
                IsSpy: showIsSpy ? p.PlayerState.IsSpy : null
            );
        }).ToList();

        // Map game settings
        var rulesDto = new SpyGameRulesDto(
            TimerMinutes: room.GameSettings.RoundDurationMinutes,
            MaxSpiesCount: room.GameSettings.MaxSpiesCount,
            MinSpiesCount: room.GameSettings.MinSpiesCount,
            MaxPlayersCount: room.GameSettings.MaxPlayerCount,
            IsSpiesKnowEachOther: room.GameSettings.SpiesKnowEachOther,
            IsShowCategoryToSpy: room.GameSettings.ShowCategoryToSpy,
            IsSpiesPlayAsTeam: room.GameSettings.SpiesPlayAsTeam
        );

        var wordPacksDto = new SpyGameWordPacksDto(
            CustomCategories: room.GameSettings.CustomCategories.Select(
                c => new WordsCategoryDto(c.Name, c.Words)).ToList());

        // Map game state
        SpyGameStateDto? gameState = null;

        if (room.Status == RoomStatus.InGame || room.Status == RoomStatus.Ended)
        {
            string? secretWord;
            string? category;

            // Secret word and category
            if (room.Status == RoomStatus.Ended)
            {
                secretWord = room.GameState.CurrentSecretWord;
                category = room.GameState.CurrentCategory;
            }
            else
            {
                secretWord = isTargetPlayerSpy ? null : room.GameState.CurrentSecretWord;

                var canSeeCategory = !isTargetPlayerSpy || room.GameSettings.ShowCategoryToSpy;
                category = canSeeCategory ? room.GameState.CurrentCategory : null;
            }

            var activeVotesCount = room.Players.Count(p => p.PlayerState.VotedToStopTimer && p.IsConnected);

            VotingStateDto? votingDto = null;

            // Voting state
            if (room.GameState.ActiveVoting != null)
            {
                var activePlayersCount = room.Players.Count(p => p.IsConnected);
                var votesRequired = (int)Math.Floor(activePlayersCount / 2.0) + 1;

                if (room.GameState.ActiveVoting is AccusationVotingState accState)
                {
                    var accusedName = room.Players.FirstOrDefault(p => p.IdInRoom == accState.TargetId)?.Name;

                    votingDto = new VotingStateDto(
                        Type: SpyVotingType.Accusation,
                        AccusedPlayerId: accState.TargetId,
                        AccusedPlayerName: accusedName,
                        TargetVoting: accState.Votes,
                        AgainstVoting: null,
                        votesRequired: votesRequired,
                        StartedAt: accState.VotingStartedAt,
                        EndsAt: accState.VotingEndsAt
                    );
                }
                else if (room.GameState.ActiveVoting is GeneralVotingState generalState)
                {
                    votingDto = new VotingStateDto(
                        Type: SpyVotingType.Final,
                        AccusedPlayerId: null,
                        AccusedPlayerName: null,
                        TargetVoting: null,
                        AgainstVoting: generalState.Votes!,
                        votesRequired: votesRequired,
                        StartedAt: generalState.VotingStartedAt,
                        EndsAt: generalState.VotingEndsAt
                    );
                }
            }

            string? caughtSpyName = null;
            if (!string.IsNullOrEmpty(room.GameState.CaughtSpyId))
            {
                caughtSpyName = room.Players.FirstOrDefault(x => x.IdInRoom == room.GameState.CaughtSpyId)?.Name;
            }

            gameState = new SpyGameStateDto(
                CurrentSecretWord: secretWord,
                CurrentCategory: category,
                RoundStartedAt: room.GameState.RoundStartedAt ?? DateTime.UtcNow,
                IsRoundTimerStopped: room.GameState.RoundTimerState.IsTimerStopped,
                RoundTimerWillStopAt: room.GameState.RoundTimerState.TimerWillStopAt,
                RoundTimerStartedAt: room.GameState.RoundTimerState.TimerStartedAt,
                RoundTimerPausedAt: room.GameState.RoundTimerState.TimerPausedAt,
                PlayersVotedToStopTimer: room.Players.Count(p => p.PlayerState.VotedToStopTimer),
                VotesRequiredToStopTimer: room.GetMajorityRequiredVotes(),
                Phase: room.GameState.CurrentPhase,
                ActiveVoting: votingDto,
                CaughtSpyId: room.GameState.CaughtSpyId,
                CaughtSpyName: caughtSpyName,
                SpyLastChanceEndsAt: room.GameState.SpyLastChanceEndsAt,
                RoundEndReason: room.GameState.GameEndReason,
                SpiecsReveal: room.GetSpyRevealDto()
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
            Rules: rulesDto,
            WordPacks: wordPacksDto,
            GameState: gameState,
            Messages: messages,
            Version: room.StateVersion
        );

        return stateDto;
    }
}
