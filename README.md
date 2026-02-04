# Follower - Twitter Growth Agent

## Overview

An email-based agent that helps grow your Twitter/X following by generating punchy, viral tweets.

**How it works:**
1. You send an email with a topic you want to tweet about
2. Agent researches the topic (web search) and generates a punchy tweet
3. Agent replies with the proposed tweet
4. You reply: `/post` (publish), `/reject` (discard), or feedback to iterate
5. On `/post` → publishes to X/Twitter

Human-in-the-loop approval ensures you stay in control while the agent does the heavy lifting.

---

## Architecture

### Email as State Store

Your mailbox serves as both communication channel and database:

```
📁 Inbox/
   └── Unread emails = topic requests from you
   └── Read emails = active tweet threads (awaiting /post or /reject)

📁 Sent/
   └── Agent's replies with proposed tweets

📁 Archive/
   └── Completed threads (posted or rejected)
```

### Workflow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  You send   │────▶│   Agent     │────▶│ Agent reply │
│  topic      │     │  researches │     │ with tweet  │
│  email      │     │  + generates│     └──────┬──────┘
└─────────────┘     └─────────────┘           │
                                              ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Post to X  │◀────│   Agent     │◀────│ Your reply  │
│             │     │  processes  │     │ /post or    │
└─────────────┘     │   command   │     │ feedback    │
                    └─────────────┘     └─────────────┘
```

---

## Commands

Reply to any tweet thread with:

| Command | Action |
|---------|--------|
| `/post` | Approve and publish tweet to X |
| `/reject` | Discard thread, move to archive |
| *(any other text)* | Feedback - agent will refine and reply again |

---

## Technical Architecture

### Stack

| Layer | Choice | Rationale |
|-------|--------|-----------|
| Runtime | .NET 10 | Minimal footprint |
| Email | MailKit | Single lib for IMAP + SMTP |
| LLM | Azure OpenAI / Ollama | GPT-4o for research + generation (Ollama for local) |
| Web Search | Azure OpenAI | Built-in web search tool for research |
| X API | Raw HttpClient | OAuth 1.0a, single endpoint |
| Scheduler | .NET BackgroundService | Built-in, no external deps |

**External dependencies: 3** (MailKit, Azure.AI.OpenAI, HtmlAgilityPack)

### Services

```
┌────────────────────────────────────────────────────────────┐
│                       Worker Service                       │
│                   (BackgroundService loop)                 │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ EmailService │  │ TweetService │  │   XService   │      │
│  ├──────────────┤  ├──────────────┤  ├──────────────┤      │
│  │ - GetUnread  │  │ - Generate   │  │ - PostTweet  │      │
│  │ - GetThreads │  │ - Refine     │  │              │      │
│  │ - Reply      │  │              │  │              │      │
│  │ - Archive    │  │      ▲       │  │              │      │
│  └──────────────┘  └──────┼───────┘  └──────────────┘      │
│                           │                                │
│                    ┌──────┴───────┐                        │
│                    │  LlmService  │                        │
│                    │ + WebSearch  │                        │
│                    └──────────────┘                        │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

### Service Responsibilities

**EmailService**
- Connect via IMAP (read) + SMTP (send)
- Get unread emails (new topic requests)
- Get active threads (awaiting commands)
- Reply to threads, move to archive

**TweetService**
- Research topic using LLM + web search
- Generate punchy, viral tweet (≤280 chars)
- Refine based on user feedback

**LlmService**
- Wrapper around Azure OpenAI SDK (with Ollama fallback)
- Web search tool for topic research
- Prompt engineering for viral tweets

**XService**
- OAuth 1.0a signature generation
- `PostTweet(text)` → success/failure
- Retry with exponential backoff

**Worker (Orchestrator)**
- Runs on timer (configurable, default 30 seconds)
- Main loop:
  1. Process new topic emails → research & generate tweet → reply
  2. Process command replies → /post, /reject, or refine

### Main Loop Pseudocode

```
every poll interval:
    # Handle new topic requests
    newTopics = EmailService.GetUnread()
    for each topic:
        research = LlmService.WebSearch(topic)
        tweet = TweetService.Generate(topic, research)
        EmailService.Reply(topic, tweet)
        EmailService.MarkAsRead(topic)

    # Handle replies to existing threads
    replies = EmailService.GetReplies()
    for each reply:
        if reply.contains("/post"):
            tweet = extractTweetFromThread(reply)
            XService.PostTweet(tweet)
            EmailService.Archive(reply.thread)

        else if reply.contains("/reject"):
            EmailService.Archive(reply.thread)

        else:  # feedback
            refined = TweetService.Refine(reply.thread, reply.feedback)
            EmailService.Reply(reply.thread, refined)
```

---

## Tweet Style Guidelines

The LLM is prompted to generate tweets that:
- Are punchy and concise (≤280 chars)
- Hook the reader in the first line
- Provide genuine insight or value
- Avoid generic platitudes
- Use active voice
- Create engagement (replies, retweets)

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Email account with IMAP/SMTP access (e.g., Gmail with App Password)
- Azure OpenAI endpoint and API key (or Ollama for local LLM)
- X/Twitter API credentials (OAuth 1.0a)

### Configuration

Copy `appsettings.json` and configure:

```json
{
  "Agent": {
    "ImapServer": "imap.gmail.com",
    "ImapPort": 993,
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 465,
    "EmailUsername": "your-email@gmail.com",
    "EmailPassword": "your-app-password",
    "LlmProvider": "Azure",
    "AzureOpenAIEndpoint": "https://your-endpoint.openai.azure.com",
    "AzureOpenAIKey": "your-api-key",
    "AzureOpenAIDeployment": "gpt-4o",
    "TwitterApiKey": "...",
    "TwitterApiSecret": "...",
    "TwitterAccessToken": "...",
    "TwitterAccessTokenSecret": "..."
  }
}
```

### Development Commands

| Command | Description |
|---------|-------------|
| `make build` | Build the project |
| `make run` | Run the agent |
| `make test` | Run all tests |
| `make clean` | Clean build artifacts |
| `make restore` | Restore NuGet packages |

Or use dotnet CLI directly:

```bash
dotnet build
dotnet run --project Follower
dotnet test
```

---

## Future Enhancements

- Multiple concurrent threads
- Scheduled posting (optimal times)
- Analytics integration
- Thread/multi-tweet support
