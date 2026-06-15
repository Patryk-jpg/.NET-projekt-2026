namespace ClinicManager.Configuration;

public class UpcomingVisitsReportOptions
{
    public bool Enabled { get; set; } = true;
    public bool RunOnStartup { get; set; } = true;
    public int IntervalHours { get; set; } = 24;
    public string OutputDirectory { get; set; } = "reports";
    public string FileName { get; set; } = "upcoming_visits.pdf";
    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "clinicmanager@localhost";
    public string To { get; set; } = "admin@clinic.local";
}
