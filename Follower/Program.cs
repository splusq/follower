using DotNetEnv;
using Follower;
using Follower.Configuration;
using Follower.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Load .env file from project directory
var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}
else if (File.Exists(".env"))
{
    Env.Load();
}

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    // Bind configuration
    services.Configure<AgentOptions>(
        context.Configuration.GetSection(AgentOptions.SectionName));

    // Register services
    services.AddSingleton<ILlmService, LlmService>();
    services.AddSingleton<IEmailService, EmailService>();
    services.AddSingleton<ITweetService, TweetService>();
    services.AddHttpClient<IXTwitterService, XTwitterService>();

    // Register hosted service
    services.AddHostedService<Worker>();
});

var host = builder.Build();
await host.RunAsync();
