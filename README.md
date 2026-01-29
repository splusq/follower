# Twitter Agent - Implementation Plan

## Overview

An email-based agent system that generates insightful tweets by combining:
- Writing style learned from top X/Twitter influencers
- Content/topics from personal notes

The agent communicates via a Yahoo email account, with human-in-the-loop approval before posting.

---

## Architecture

### Email as State Store

The Yahoo mailbox serves as both communication channel and database:

```
📁 Drafts/
   └── Knowledge base: health & software engineering notes
       - User pushes content async by saving drafts
       - Agent reads drafts as source material
       - Max 5 tweets generated per draft

📁 Influencers/
   └── Curated tweets for style learning
       - User manually adds tweets they admire
       - Each email = one or more example tweets
       - Static collection, updated at user's discretion
       - No X API needed for reading

📁 Inbox/
   └── Active tweet threads
       - Subject line = unique topic identifier
       - Thread continues until /post or /reject

📁 Sent/
   └── Agent's outbound messages

📁 Archive/ (or Posted/)
   └── Completed threads after posting
```

### Agents

1. **Style Agent**
   - Reads curated tweets from Influencers folder
   - Builds voice profile: tone, structure, vocabulary, patterns
   - No X API dependency - user manually curates examples
   - Output: style guidelines for content generation

2. **Content Agent**
   - Reads notes from Drafts folder
   - Generates tweet ideas from topics
   - Applies style guidelines from Style Agent
   - Output: draft tweets for review

### Workflow

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│   Drafts    │────▶│   Agent     │────▶│  Send Email │
│  (notes)    │     │  generates  │     │  to user    │
└─────────────┘     │   tweet     │     └──────┬──────┘
                    └─────────────┘            │
                                               ▼
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Post to X  │◀────│   Agent     │◀────│ User reply  │
│             │     │  processes  │     │ /post or    │
└─────────────┘     │   command   │     │ feedback    │
                    └─────────────┘     └─────────────┘
```

---

## Constraints & Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| Tweets per draft | 5 max | Prevents over-mining single topic |
| Daily tweet generation | 1-2 | Agent-initiated |
| Active threads | 1 | Designed for future concurrency |
| Style examples | User-curated | Maintained in Influencers folder |

---

## Commands

User replies with commands to control flow:

| Command | Action |
|---------|--------|
| `/post` | Approve and publish tweet to X |
| `/reject` | Kill thread, archive without posting |
| *(any other text)* | Feedback for iteration |

---

## Subject Line Convention

Format: `TWEET-{draft-id}-{sequence}: {summary}`

Example: `TWEET-a1b2c3-2: On the hidden cost of microservices`

- `draft-id`: Hash/identifier of source draft
- `sequence`: Which tweet (1-5) from this draft
- `summary`: Brief description for human readability

---

## Technical Architecture

### Stack

| Layer | Choice | Rationale |
|-------|--------|-----------|
| Runtime | .NET 10 | Minimal footprint, latest LTS |
| Email | MailKit | Single lib for IMAP + SMTP, well-maintained |
| LLM | Anthropic SDK | Claude for style analysis + generation |
| X API | Raw HttpClient | OAuth 1.0a, no SDK needed for single endpoint |
| Scheduler | .NET BackgroundService | Built-in, no external deps |
| Config | appsettings.json + env vars | Secrets in env, tunables in config |

**Total external dependencies: 2** (MailKit, Anthropic SDK)

### Services (Single Process)

```
┌─────────────────────────────────────────────────────────────┐
│                        Worker Service                        │
│                    (BackgroundService loop)                  │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ EmailService │  │ StyleService │  │ TweetService │       │
│  ├──────────────┤  ├──────────────┤  ├──────────────┤       │
│  │ - ReadInbox  │  │ - Analyze    │  │ - Generate   │       │
│  │ - ReadDrafts │  │ - GetProfile │  │ - Refine     │       │
│  │ - ReadFolder │  │              │  │              │       │
│  │ - Send       │  │      ▲       │  │      ▲       │       │
│  │ - Archive    │  │      │       │  │      │       │       │
│  └──────┬───────┘  └──────┼───────┘  └──────┼───────┘       │
│         │                 │                 │                │
│         │          ┌──────┴─────────────────┴──────┐        │
│         │          │         LlmService            │        │
│         │          │  (Anthropic Claude wrapper)   │        │
│         │          └───────────────────────────────┘        │
│         │                                                    │
│         │          ┌───────────────────────────────┐        │
│         └─────────▶│        XTwitterService        │        │
│                    │  (OAuth 1.0a, post endpoint)  │        │
│                    └───────────────────────────────┘        │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Service Responsibilities

**EmailService**
- Connect to Yahoo via IMAP (read) + SMTP (send)
- Read specific folders: Inbox, Drafts, Influencers, Archive
- Parse email bodies and extract commands
- Send new emails, move emails between folders

**StyleService**
- Fetch all emails from Influencers folder via EmailService
- Build style prompt/profile using LlmService
- Cache result (optional, in-memory or file)

**TweetService**
- Pick random draft via EmailService
- Generate tweet using LlmService + style profile
- Refine tweet based on user feedback

**LlmService**
- Thin wrapper around Anthropic SDK
- Two main calls: `AnalyzeStyle(examples)` → profile, `GenerateTweet(notes, style)` → tweet

**XTwitterService**
- OAuth 1.0a signature generation
- Single method: `PostTweet(text)` → success/failure
- Retry with exponential backoff

**Worker (Orchestrator)**
- Runs on timer (check every N minutes)
- Main loop:
  1. Check inbox for replies → process commands
  2. If no active thread & time for new tweet → generate one
  3. Handle /post → call XTwitterService, archive on success
  4. Handle /reject → archive without posting
  5. Handle feedback → refine and reply

### Main Loop Pseudocode

```
every 5 minutes:
    replies = EmailService.GetUnreadReplies()

    for each reply:
        if reply.contains("/post"):
            tweet = extractTweetFromThread(reply)
            XTwitterService.PostWithRetry(tweet)
            EmailService.MoveToArchive(reply.thread)

        else if reply.contains("/reject"):
            EmailService.MoveToArchive(reply.thread)

        else:  # feedback
            style = StyleService.GetProfile()
            refined = TweetService.Refine(reply.feedback, style)
            EmailService.Reply(reply.thread, refined)

    if noActiveThread() and shouldGenerateToday():
        style = StyleService.GetProfile()
        draft = EmailService.GetRandomDraft()
        tweet = TweetService.Generate(draft, style)
        EmailService.SendNew(tweet, subject=generateSubject(draft))
```

---

## Components to Build

### 1. Email Service
- [ ] POP3/IMAP client for reading inbox and drafts
- [ ] SMTP client for sending emails
- [ ] Parser for extracting commands from replies
- [ ] Thread tracking by subject line

### 2. Style Agent
- [ ] Influencers folder reader (email parsing)
- [ ] Style analysis and profile generation
- [ ] Refresh when folder contents change

### 3. Content Agent
- [ ] Draft folder reader
- [ ] Topic extraction from notes
- [ ] Tweet generation with style application
- [ ] Draft usage tracking (count toward 5 max)

### 4. X/Twitter Integration (Write-Only, Free Tier)
- [ ] OAuth 1.0a setup with "Read and Write" permissions
- [ ] Tweet posting API call (v2 endpoint)
- [ ] Retry logic with exponential backoff
- [ ] Only confirm success after 200 response
- Note: Free tier = 1,500 tweets/month (plenty for 1-2/day)

### 5. Orchestrator
- [ ] Scheduler for 1-2 daily generations
- [ ] State management (active thread tracking)
- [ ] Command routing (/post, /reject, feedback)

---

## Resolved Questions

1. **X API Access** ✅
   - **Free tier ($0)** is sufficient for posting
   - Limit: 1,500 tweets/month (we need ~30-60)
   - Requires OAuth 1.0a with "Read and Write" permissions
   - No read API access needed (style examples are manually curated)

2. **Draft Exhaustion** ✅
   - Agent randomly picks from available drafts
   - When tweet is posted, archive tracks usage via subject prefix
   - Count archived emails with same prefix to determine if draft is exhausted (5 max)
   - Simple heuristic: `TWEET-{draft-subject-hash}-*` count in Archive

3. **Style Refresh** ✅
   - On-demand: regenerate style profile before each tweet generation
   - Optional: local filesystem cache for performance (not required initially)
   - Cache invalidation: rebuild when Influencers folder changes

4. **Error Recovery** ✅
   - On X post failure: keep retrying (with backoff)
   - Thread stays in Inbox until successful post
   - Only move to Archive/Posted after confirmed success
   - User sees thread persist until it actually posts

---

## Future Enhancements

- Multiple concurrent tweet threads
- Priority levels for drafts
- Scheduled posting (not just immediate)
- Analytics: which topics perform well
- Thread/multi-tweet support
