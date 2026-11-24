using System.Threading.Channels;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RutaSV.Application.Identity.Service;

namespace RutaSV.Infrastructure.Email;

public interface IBackgroundEmailQueue
{
    void Enqueue(EmailNotificationRequest request);
    IAsyncEnumerable<EmailNotificationRequest> ReadAllAsync(CancellationToken cancellationToken);
}

public sealed class BackgroundEmailQueue : IBackgroundEmailQueue
{
    private readonly Channel<EmailNotificationRequest> _channel =
        Channel.CreateUnbounded<EmailNotificationRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public void Enqueue(EmailNotificationRequest request)
    {
        _ = _channel.Writer.TryWrite(request);
    }

    public IAsyncEnumerable<EmailNotificationRequest> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}

public sealed class BackgroundEmailWorker(
    IBackgroundEmailQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (EmailNotificationRequest request in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                using IServiceScope scope = scopeFactory.CreateScope();
                SmtpEmailService smtpService = scope.ServiceProvider.GetRequiredService<SmtpEmailService>();
                await smtpService.DeliverNotificationNowAsync(request, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error al enviar notificación en segundo plano ({Type}) a {Recipient}",
                    request.Type,
                    request.RecipientEmail);
            }
        }
    }
}
