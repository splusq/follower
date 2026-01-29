using MailKit;
using MailKit.Net.Pop3;
using MimeKit;

namespace Follower.Services;

public class Pop3EmailService
{
    private readonly string _server;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly SmtpMessageProcessor _messageProcessor;

    public Pop3EmailService(string server, int port, string username, string password, SmtpMessageProcessor messageProcessor)
    {
        _server = server;
        _port = port;
        _username = username;
        _password = password;
        _messageProcessor = messageProcessor;
    }

    public async Task ProcessEmailsAsync()
    {
        using var client = new Pop3Client()
        {
            ServerCertificateValidationCallback = (s, c, h, e) => true
        };

        try
        {
            Console.WriteLine($"Connecting to {_server}:{_port}...");
            
            // Use explicit SSL/TLS
            await client.ConnectAsync(_server, _port, MailKit.Security.SecureSocketOptions.SslOnConnect);
            
            await client.AuthenticateAsync(_username, _password);
            
            Console.WriteLine("Authentication successful!");

            Console.WriteLine($"Messages in inbox: {client.Count}");

            for (int i = 0; i < client.Count; i++)
            {
                var message = await client.GetMessageAsync(i);
                Console.WriteLine($"Processing email: {message.Subject}");
            
                // Convert MimeMessage to raw content for processing
                using var stream = new MemoryStream();
                await message.WriteToAsync(stream);
                stream.Position = 0;
                
                // Process the email using existing SmtpMessageProcessor
                await _messageProcessor.SaveAsync(stream, CancellationToken.None);
                
                // Mark the message for deletion
                await client.DeleteMessageAsync(i);
            }

            await client.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }
}
