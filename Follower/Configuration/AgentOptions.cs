namespace Follower.Configuration;

public class AgentOptions
{
    public const string SectionName = "Agent";

    // Polling
    public int PollIntervalSeconds { get; set; } = 30;

    // Email folders
    public string ArchiveFolder { get; set; } = "Archive";

    // IMAP settings
    public string ImapServer { get; set; } = "";
    public int ImapPort { get; set; } = 993;

    // SMTP settings
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; } = 465;

    // Email credentials
    public string EmailUsername { get; set; } = "";
    public string EmailPassword { get; set; } = "";

    // LLM Provider: "Azure" or "Ollama"
    public string LlmProvider { get; set; } = "Azure";

    // Azure OpenAI
    public string AzureOpenAIEndpoint { get; set; } = "";
    public string AzureOpenAIKey { get; set; } = "";
    public string AzureOpenAIDeployment { get; set; } = "gpt-4o";

    // Web Search (built into Azure OpenAI Responses API)
    public bool EnableWebSearch { get; set; } = true;

    // Ollama (local)
    public string OllamaEndpoint { get; set; } = "http://localhost:11434/v1";
    public string OllamaModel { get; set; } = "llama3.2";

    // Twitter/X API (OAuth 1.0a)
    public string TwitterApiKey { get; set; } = "";
    public string TwitterApiSecret { get; set; } = "";
    public string TwitterAccessToken { get; set; } = "";
    public string TwitterAccessTokenSecret { get; set; } = "";
}
