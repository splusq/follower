using Follower.Models;

namespace Follower.Services;

public class StyleService : IStyleService
{
    private readonly IEmailService _emailService;
    private readonly ILlmService _llmService;

    public StyleService(IEmailService emailService, ILlmService llmService)
    {
        _emailService = emailService;
        _llmService = llmService;
    }

    public Task<StyleProfile> GetStyleProfileAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
