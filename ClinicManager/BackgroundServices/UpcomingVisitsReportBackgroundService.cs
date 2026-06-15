using System.Net;
using System.Net.Mail;
using ClinicManager.Configuration;
using ClinicManager.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace ClinicManager.BackgroundServices;

public class UpcomingVisitsReportBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<UpcomingVisitsReportOptions> options,
    IWebHostEnvironment environment,
    ILogger<UpcomingVisitsReportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Raport nadchodzacych wizyt jest wylaczony w konfiguracji.");
            return;
        }

        if (settings.RunOnStartup)
        {
            await GenerateAndDeliverAsync(stoppingToken);
        }

        var interval = TimeSpan.FromHours(Math.Max(1, settings.IntervalHours));
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await GenerateAndDeliverAsync(stoppingToken);
        }
    }

    private async Task GenerateAndDeliverAsync(CancellationToken cancellationToken)
    {
        try
        {
            var reportDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
            using var scope = scopeFactory.CreateScope();
            var reportService = scope.ServiceProvider.GetRequiredService<IUpcomingVisitsReportService>();
            var pdf = await reportService.GeneratePdfAsync(reportDate, cancellationToken);

            var smtp = options.Value.Smtp;
            if (smtp.Enabled)
            {
                await SendEmailAsync(pdf, reportDate, smtp, cancellationToken);
                logger.LogInformation("Wyslano raport nadchodzacych wizyt na {Date}.", reportDate);
            }
            else
            {
                var path = SaveTestFile(pdf);
                logger.LogInformation("Zapisano testowy raport nadchodzacych wizyt: {Path}.", path);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Nie udalo sie wygenerowac raportu nadchodzacych wizyt.");
        }
    }

    private string SaveTestFile(byte[] pdf)
    {
        var settings = options.Value;
        var directory = Path.IsPathRooted(settings.OutputDirectory)
            ? settings.OutputDirectory
            : Path.Combine(environment.ContentRootPath, settings.OutputDirectory);
        Directory.CreateDirectory(directory);

        var path = Path.Combine(directory, settings.FileName);
        File.WriteAllBytes(path, pdf);
        return path;
    }

    private static async Task SendEmailAsync(
        byte[] pdf,
        DateOnly reportDate,
        SmtpOptions smtp,
        CancellationToken cancellationToken)
    {
        using var message = new MailMessage(smtp.From, smtp.To)
        {
            Subject = $"Raport wizyt na {reportDate:yyyy-MM-dd}",
            Body = "W zalaczniku znajduje sie raport jutrzejszych wizyt."
        };
        message.Attachments.Add(new Attachment(
            new MemoryStream(pdf),
            "upcoming_visits.pdf",
            "application/pdf"));

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl
        };
        if (!string.IsNullOrWhiteSpace(smtp.UserName))
        {
            client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}
