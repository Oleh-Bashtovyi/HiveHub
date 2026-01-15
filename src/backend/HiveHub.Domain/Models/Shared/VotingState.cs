namespace HiveHub.Domain.Models.Shared;

public enum TargetVoteType
{
    Yes,
    No,
    Skip
}

public abstract class VotingStateBase
{
    public DateTime VotingStartedAt { get; set; }
    public DateTime VotingEndsAt { get; set; }
}

public sealed class AccusationVotingState : VotingStateBase
{
    public required string InitiatorId { get; init; }
    public required string TargetId { get; init; }
    public Dictionary<string, TargetVoteType> Votes { get; set; } = new();

    public bool TryVote(string voterPlayerId, TargetVoteType voteType)
    {
        return Votes.TryAdd(voterPlayerId, voteType);
    }
}

public sealed class GeneralVotingState : VotingStateBase
{
    public Dictionary<string, string?> Votes { get; set; } = new();

    public bool TryVote(string voterPlayerId, string targetPlayerId)
    {
        return Votes.TryAdd(voterPlayerId, targetPlayerId);
    }
}

