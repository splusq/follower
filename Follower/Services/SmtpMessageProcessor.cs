using MimeKit;
using Follower.Utils;

namespace Follower.Services;

/// <summary>
/// Legacy message processor - to be replaced by LlmService.
/// </summary>
public class SmtpMessageProcessor
{
    public SmtpMessageProcessor()
    {
    }

    public async Task SaveAsync(Stream messageStream, CancellationToken cancellationToken)
    {
        try
        {
            Console.WriteLine("Processing new message...");

            // Parse the email message
            var message = await MimeMessage.LoadAsync(messageStream, cancellationToken);
            var textContent = "";

            // Extract text content from the message
            foreach (var part in message.BodyParts.OfType<TextPart>())
            {
                if (part.IsHtml)
                {
                    // Convert HTML to Markdown
                    textContent = HtmlConverter.ToMarkdown(part.Text);
                    break;
                }
                else if (part.IsPlain)
                {
                    textContent = part.Text;
                    break;
                }
            }

            if (string.IsNullOrEmpty(textContent))
            {
                Console.WriteLine("No text content found in the message.");
                return;
            }

            Console.WriteLine($"Found text content ({textContent.Length} chars).");

            // TODO: Replace with ILlmService call
            throw new NotImplementedException("LLM processing not yet implemented - use ILlmService");
        }
        catch (NotImplementedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            throw;
        }
    }
}
