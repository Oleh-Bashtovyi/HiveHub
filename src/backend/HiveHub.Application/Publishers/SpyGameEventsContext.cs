using HiveHub.Application.Dtos.Shared;
using HiveHub.Application.Dtos.SpyGame;
using HiveHub.Application.Services;
using Microsoft.Extensions.Logging;

namespace HiveHub.Application.Publishers;

public class SpyGameEventsContext(
    ISpyGamePublisher publisher,
    ILogger<SpyGameEventsContext> logger,
    ITaskScheduler scheduler) : BaseEventsContext(publisher, scheduler, logger)
{
    public override async Task<bool> HandleEvent(IRoomEvent roomEvent)
    {
        switch (roomEvent)
        {
            // Connection
            case PlayerJoinedEventDto<SpyPlayerDto> e:
                await publisher.PublishPlayerJoinedAsync(e);
                break;

            // Lobby
            case SpyGameWordPacksUpdatedEventDto e:
                await publisher.PublishWordPacksUpdatedAsync(e);
                break;

            case SpyGameRulesUpdatedEventDto e:
                await publisher.PublishGameRulesUpdatedAsync(e);
                break;

            case TargetedGameStartedEventDto e:
                await publisher.PublishGameStartedAsync(e.ConnectionId, e.Payload);
                break;

            // Gameplay
            case PlayerVotedToStopTimerEventDto e:
                await publisher.PublishTimerVoteAsync(e);
                break;

            case SpyGameEndedEventDto e:
                await publisher.PublishGameEndedAsync(e);
                break;

            case VotingStartedEventDto e:
                await publisher.PublishVotingStartedAsync(e);
                break;

            case VoteCastEventDto e:
                await publisher.PublishVoteCastAsync(e);
                break;

            case ChatMessageEventDto e:
                await publisher.PublishChatMessageAsync(e);
                break;

            case SpyGameRoundTimerStateChangedEventDto e:
                await publisher.PublishTimerStateChangedAsync(e);
                break;

            case GamePhaseChangedEventDto e:
                await publisher.PublishGamePhaseChangedAsync(e);
                break;

            case VotingCompletedEventDto e:
                await publisher.PublishVotingCompletedAsync(e);
                break;

            case PlayerEliminatedEventDto e:
                await publisher.PublishPlayerEliminatedAsync(e);
                break;

            case SpyRevealedEventDto e:
                await publisher.PublishSpyRevealedAsync(e);
                break;

            case SpyLastChanceStartedEventDto e:
                await publisher.PublishSpyLastChanceStartedAsync(e);
                break;

            case SpyGuessAttemptedEventDto e:
                await publisher.PublishSpyGuessAttemptedAsync(e);
                break;

            default:
                return false;
        }
        return true;
    }
}
