namespace Follower.Configuration;

public class AgentOptions
{
    public const string SectionName = "Agent";

    public int PollIntervalSeconds { get; set; } = 300;
    public int MaxTweetsPerDraft { get; set; } = 5;
    public int DailyTweetTarget { get; set; } = 2;
    public string ArchiveFolder { get; set; } = "Archive";
    public string DraftsFolder { get; set; } = "Drafts";
    public string InfluencersFolder { get; set; } = "Influencers";
}
