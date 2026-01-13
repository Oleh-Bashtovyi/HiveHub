using FluentResults;
using HiveHub.Application.Constants;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Extensions;
using HiveHub.Application.Models;
using HiveHub.Application.Publishers;
using HiveHub.Application.Services;
using HiveHub.Application.Utils;
using HiveHub.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.MediatR.SpyGame.Commands.Vote;

public record VoteCommand(
    string RoomCode,
    string ConnectionId,
    string TargetPlayerId,
    TargetVoteType? VoteType
) : IRequest<Result>;

public class VoteHandler(
    ISpyGameRepository repository,
    ISpyGamePublisher publisher,
    ITaskScheduler scheduler,
    ILogger<VoteHandler> logger) : IRequestHandler<VoteCommand, Result>
{
    public async Task<Result> Handle(VoteCommand request, CancellationToken token)
    {
        if (!repository.TryGetRoom(request.RoomCode, out var roomAccessor))
        {
            return Results.NotFound(ProjectMessages.RoomNotFound);
        }

        bool shouldResolve = false;
        string voterId = string.Empty;

        var result = await roomAccessor.ExecuteAsync(async (room) => 
        {
            if (room.ActiveVoting == null)
            {
                return Results.ActionFailed(ProjectMessages.Accusation.NoActiveVoting);
            }

            if (!room.TryGetPlayerByConnectionId(request.ConnectionId, out var voter))
            {
                return Results.NotFound(ProjectMessages.Accusation.InitiatorNotFound);
            }

            if (string.IsNullOrEmpty(request.TargetPlayerId))
            {
                return Results.ActionFailed(ProjectMessages.Accusation.TargetPlayerIdrequiredForFinalVote);
            }

            if (!room.TryGetPlayerByIdInRoom(request.TargetPlayerId, out var targetPlayer))
            {
                return Results.NotFound(ProjectMessages.Accusation.TargetNotFound);
            }

            voterId = voter.IdInRoom;

            if (room.CurrentPhase == SpyGamePhase.Accusation && room.ActiveVoting is AccusationVotingState accusationState)
            {
                if (request.VoteType == null)
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.VoteTypeWasNotSpecified);
                }
                if (request.TargetPlayerId != accusationState.TargetId)
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.VotingTargetMismatch);
                }
                if (!accusationState.TryVote(voter.IdInRoom, request.VoteType.Value))
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.YouAlreadyVoted);
                }
            }
            else if (room.CurrentPhase == SpyGamePhase.FinalVote && room.ActiveVoting is GeneralVotingState finalState)
            {
                if (!finalState.TryVote(voter.IdInRoom, targetPlayer.IdInRoom))
                {
                    return Results.ActionFailed(ProjectMessages.Accusation.YouAlreadyVoted);
                }
            }
            else
            {
                return Results.ActionFailed(ProjectMessages.Accusation.UnknownVotingState);
            }

            var activePlayersCount = room.Players.Count(p => p.IsConnected);
            var currentVotesCount = room.ActiveVoting switch
            {
                AccusationVotingState s1 => s1.Votes.Count,
                GeneralVotingState s2 => s2.Votes.Count,
                _ => 0
            };

            if (currentVotesCount >= activePlayersCount)
            {
                shouldResolve = true;
            }

            return Result.Ok();
        });

        if (result.IsFailed)
        {
            return result;
        }

        await publisher.PublishVoteCastAsync(new VoteCastEventDto(
            RoomCode: request.RoomCode, 
            VoterId: voterId,
            TargetVoteType: request.VoteType,
            AgainstPlayerId: request.TargetPlayerId));

        if (shouldResolve)
        {
            await scheduler.CancelAsync(new ScheduledTask(TaskType.SpyVotingTimeUp, request.RoomCode, null));
            await ResolveVoting(repository, request.RoomCode);
        }

        return Result.Ok();
    }

    private async Task ResolveVoting(ISpyGameRepository repository, string roomCode)
    {
        var roomAccessor = repository.GetRoom(roomCode);
        if (roomAccessor == null) return;

        VotingResultEventDto? resultDto = null;
        GameEndedEventDto? gameEndedDto = null;
        ScheduledTask? timerResumeTask = null;
        TimeSpan? timerResumeDelay = null;

        await roomAccessor.ExecuteAsync(async (room) =>
        {
            var activePlayers = room.Players.Where(p => p.IsConnected).ToList();
            int requiredVotes = (int)Math.Floor(activePlayers.Count / 2.0) + 1;

            // --- RESOLVE ACCUSATION ---
            if (room.CurrentPhase == SpyGamePhase.Accusation && room.ActiveVoting is AccusationVotingState accState)
            {
                var yesVotes = accState.Votes.Count(v => v.Value == TargetVoteType.Yes);

                if (yesVotes >= requiredVotes)
                {
                    // Звинувачення прийнято
                    var accused = room.Players.FirstOrDefault(p => p.IdInRoom == accState.TargetId);
                    if (accused != null && accused.PlayerState.IsSpy)
                    {
                        // ВПІЙМАЛИ ШПИГУНА -> Останній шанс
                        room.CurrentPhase = SpyGamePhase.SpyLastChance;
                        room.CaughtSpyId = accused.IdInRoom;
                        room.ActiveVoting = null;

                        resultDto = new VotingResultEventDto(
                            RoomCode: roomCode,
                            IsSuccess: true,
                            CurrentGamePhase: SpyGamePhase.SpyLastChance,
                            ResultMessage: $"Spy {accused.Name} caught! They have a last chance to guess the location.", 
                            AccusedId: accused.IdInRoom);
                    }
                    else
                    {
                        // ПОМИЛКА -> Мирні програли
                        room.Status = RoomStatus.Ended;
                        room.ActiveVoting = null;
                        room.WinnerTeam = Team.Spies;
                        room.GameEndReason = GameEndReason.CivilianKicked;

                        resultDto = new VotingResultEventDto(
                            RoomCode: roomCode,
                            IsSuccess: true,
                            CurrentGamePhase: SpyGamePhase.None,
                            ResultMessage: $"Player {accused?.Name} was NOT a spy. Spies win!",
                            AccusedId: accState.TargetId);

                        gameEndedDto = new GameEndedEventDto(roomCode, Team.Spies, GameEndReason.CivilianKicked, "Innocent player kicked");
                    }
                }
                else
                {
                    // Голосування провалено -> Продовжуємо гру
                    room.CurrentPhase = SpyGamePhase.Search;
                    room.ActiveVoting = null;

                    resultDto = new VotingResultEventDto(
                        RoomCode: roomCode,
                        IsSuccess: false,
                        CurrentGamePhase: SpyGamePhase.Search,
                        ResultMessage: "Not enough votes. Game resumes.",
                        AccusedId: null);

                    // Відновлюємо таймер
                    if (room.TimerState.TimerStoppedAt.HasValue && room.TimerState.PlannedGameEndTime.HasValue)
                    {
                        var timeSpentPaused = DateTime.UtcNow - room.TimerState.TimerStoppedAt.Value;
                        room.TimerState.PlannedGameEndTime = room.TimerState.PlannedGameEndTime.Value.Add(timeSpentPaused);
                        room.TimerState.IsTimerStopped = false;
                        room.TimerState.TimerStoppedAt = null;

                        var remaining = room.TimerState.PlannedGameEndTime.Value - DateTime.UtcNow;
                        timerResumeDelay = remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
                        timerResumeTask = new ScheduledTask(TaskType.SpyGameEndTimeUp, roomCode, null);
                    }
                }
            }
            // --- RESOLVE FINAL VOTE ---
            else if (room.CurrentPhase == SpyGamePhase.FinalVote && room.ActiveVoting is GeneralVotingState finalState)
            {
                var groupedVotes = finalState.Votes
                    .Where(x => !string.IsNullOrEmpty(x.Value))
                    .GroupBy(x => x.Value!)
                    .OrderByDescending(g => g.Count())
                    .ToList();

                // Якщо голосів немає або лідер не набрав більшості
                if (!groupedVotes.Any() || groupedVotes.First().Count() < requiredVotes)
                {
                    // Не домовились -> Шпигуни виграли
                    room.Status = RoomStatus.Ended;
                    room.ActiveVoting = null;
                    room.WinnerTeam = Team.Spies;
                    room.GameEndReason = GameEndReason.FinalVotingFailed;

                    resultDto = new VotingResultEventDto(
                        RoomCode: roomCode,
                        IsSuccess: false,
                        CurrentGamePhase: SpyGamePhase.None,
                        ResultMessage: "Consensus not reached. Spies win!",
                        AccusedId: null);

                    gameEndedDto = new GameEndedEventDto(roomCode, Team.Spies, GameEndReason.FinalVotingFailed, "Civilians failed to agree on a suspect");
                }
                else
                {
                    // Обрали когось
                    var targetId = groupedVotes.First().Key;
                    var target = room.Players.FirstOrDefault(p => p.IdInRoom == targetId);

                    if (target != null && target.PlayerState.IsSpy)
                    {
                        // Впіймали шпигуна -> Останній шанс
                        room.CurrentPhase = SpyGamePhase.SpyLastChance;
                        room.CaughtSpyId = targetId;
                        room.ActiveVoting = null;

                        resultDto = new VotingResultEventDto(
                            RoomCode: roomCode, 
                            IsSuccess: true,
                            CurrentGamePhase: SpyGamePhase.SpyLastChance,
                            ResultMessage: "Spy identified! Last chance to guess.", 
                            AccusedId: targetId);
                    }
                    else
                    {
                        // Помилились -> Шпигуни виграли
                        room.Status = RoomStatus.Ended;
                        room.ActiveVoting = null;
                        room.WinnerTeam = Team.Spies;
                        room.GameEndReason = GameEndReason.CivilianKicked;

                        resultDto = new VotingResultEventDto(
                            RoomCode: roomCode,
                            IsSuccess: true,
                            CurrentGamePhase: SpyGamePhase.None,
                            ResultMessage: $"Wrong choice! {target?.Name} is innocent. Spies win!",
                            AccusedId: targetId);

                        gameEndedDto = new GameEndedEventDto(roomCode, Team.Spies, GameEndReason.CivilianKicked, "Civilians voted for an innocent player");
                    }
                }
            }

            return Result.Ok();
        });

        // --- Side Effects ---
        if (resultDto != null) await publisher.PublishVotingResultAsync(resultDto);
        if (gameEndedDto != null) await publisher.PublishGameEndedAsync(gameEndedDto);

        if (timerResumeTask != null && timerResumeDelay.HasValue)
        {
            await scheduler.ScheduleAsync(timerResumeTask, timerResumeDelay.Value);
        }
    }
}
