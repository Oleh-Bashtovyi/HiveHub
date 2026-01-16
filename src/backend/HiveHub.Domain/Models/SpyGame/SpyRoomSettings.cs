namespace HiveHub.Domain.Models.SpyGame;

public sealed class SpyRoomSettings
{
    public int RoundDurationMinutes { get; set; } = 5;
    public int MinSpiesCount { get; set; } = 1;
    public int MaxSpiesCount { get; set; } = 1;
    public int MaxPlayerCount { get; set; } = 5;
    public bool SpiesKnowEachOther { get; set; } = false;
    public bool ShowCategoryToSpy { get; set; } = false;
    // If some spy fail to guess word it is game over to all of them 
    public bool SpiesPlayAsTeam { get; set; } = false;
    public List<SpyGameWordsCategory> CustomCategories { get; set; } = new();
   
    // EXPERIMENTAL. TODO LATER
    public bool RandomiseSettingsOnStart { get; private set; } = false;
}

public sealed class SpyGameWordsCategory
{
    public string Name { get; set; } = string.Empty;
    public List<string> Words { get; set; } = new();
}