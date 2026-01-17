using HiveHub.Domain.Models.Shared;

namespace HiveHub.Application.Utils;

public record VoteResult(int YesVotes, bool HasMajority, bool IsComplete, bool CanStillReachMajority);

public static class VoteCounter
{
    public static VoteResult CountVotes(
        Dictionary<string, TargetVoteType> votes,
        int totalPlayers,
        int requiredVotes)
    {
        var yesVotes = votes.Count(v => v.Value == TargetVoteType.Yes);
        var canReachMajority = yesVotes + (totalPlayers - votes.Count) >= requiredVotes;

        return new VoteResult(
            YesVotes: yesVotes,
            HasMajority: yesVotes >= requiredVotes,
            IsComplete: votes.Count >= totalPlayers,
            CanStillReachMajority: canReachMajority
        );
    }
}