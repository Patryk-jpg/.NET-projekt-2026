using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClinicManager.BackgroundServices;
using ClinicManager.Components;
using ClinicManager.Components.Account;
using ClinicManager.Configuration;
using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Endpoints;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Mappers;
using ClinicManager.Infrastructure.Services;
using ClinicManager.Services;
using NLog.Web;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddOpenApi();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                       throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// Fabryka rejestruje DbContextOptions jako singleton i pozwala serwisom domenowym
// (np. PatientService) tworzyc krotkozyjacy DbContext per operacja - zalecany pattern
// dla Blazor Server.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
// Identity i `IdentitySeeder` oczekuja scoped ApplicationDbContext - dostarczamy go
// z tej samej fabryki, zeby zachowac wspolne opcje i nie podwajac konfiguracji.
builder.Services.AddScoped<ApplicationDbContext>(sp =>
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Domain services i mappery
builder.Services.AddSingleton<PatientMapper>();
builder.Services.AddSingleton<MedicalRecordMapper>();
builder.Services.AddSingleton<VisitMapper>();
builder.Services.AddSingleton<ProcedureMapper>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IMedicalRecordService>(sp => new MedicalRecordService(
    sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>(),
    sp.GetRequiredService<MedicalRecordMapper>(),
    sp.GetRequiredService<IWebHostEnvironment>().WebRootPath));
builder.Services.AddScoped<IVisitService, VisitService>();
builder.Services.AddScoped<IProcedureService, ProcedureService>();
builder.Services.AddScoped<IMedicationService, MedicationService>();
builder.Services.AddScoped<IVisitMedicalService, VisitMedicalService>();
builder.Services.AddScoped<IVisitPdfService, VisitPdfService>();
builder.Services.AddScoped<ICostReportService, CostReportService>();
builder.Services.AddScoped<ICostReportPdfService, CostReportPdfService>();
builder.Services.Configure<UpcomingVisitsReportOptions>(
    builder.Configuration.GetSection("UpcomingVisitsReport"));
builder.Services.AddScoped<IUpcomingVisitsReportService, UpcomingVisitsReportService>();
builder.Services.AddHostedService<UpcomingVisitsReportBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapOpenApi();
app.MapVisitsApi();
app.MapGet("/visits/{id:int}/pdf", async (
        int id,
        IVisitPdfService visitPdfService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var pdf = await visitPdfService.GenerateVisitCardAsync(id, cancellationToken);
            return Results.File(pdf, "application/pdf", $"karta-wizyty-{id}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    })
    .RequireAuthorization(policy => policy.RequireRole(Roles.Lekarz, Roles.Admin));
app.MapGet("/reports/costs/pdf", async (
        int? patientId,
        int? doctorId,
        int? year,
        int? month,
        ICostReportPdfService costReportPdfService,
        CancellationToken cancellationToken) =>
    {
        try
        {
            var pdf = await costReportPdfService.GenerateAsync(new CostReportFilterDto
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Year = year,
                Month = month
            }, cancellationToken);
            return Results.File(pdf, "application/pdf", "raport-kosztow.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ex.Message);
        }
    })
    .RequireAuthorization(policy => policy.RequireRole(Roles.Admin));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

await IdentitySeeder.SeedAsync(app.Services);

app.Run();
