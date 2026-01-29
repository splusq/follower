namespace Follower.Configuration;

public class AgentOptions
{
    public const string SectionName = "Agent";

    // Polling
    public int PollIntervalSeconds { get; set; } = 300;
    public int MaxTweetsPerDraft { get; set; } = 5;
    public int DailyTweetTarget { get; set; } = 2;

    // Email folders
    public string ArchiveFolder { get; set; } = "Archive";
    public string DraftsFolder { get; set; } = "Drafts";
    public string InfluencersFolder { get; set; } = "Influencers";

    // IMAP settings
    public string ImapServer { get; set; } = "";
    public int ImapPort { get; set; } = 993;

    // SMTP settings
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 465;

    // Email credentials
    public string EmailUsername { get; set; } = "";
    public string EmailPassword { get; set; } = "";

    // Azure OpenAI
    public string AzureOpenAIEndpoint { get; set; } = "";
    public string AzureOpenAIKey { get; set; } = "";
    public string AzureOpenAIDeployment { get; set; } = "gpt-4o";
}
