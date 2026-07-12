using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using System.Security.Claims;
using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using BcgHub.Api.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Radixal.BPC.Configuration;
using Radixal.BPC.DependencyInjection;
using Radixal.BPC.Logging;
using Radixal.BPC.Swagger;

var builder = WebApplication.CreateBuilder(args);
var pathBase = builder.Configuration["ASPNETCORE_PATHBASE"]?.TrimEnd('/');
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Connection string 'DefaultConnection' is required. Configure it through secrets or environment variables.");
var configurations = builder.Services.AddRadixalBpcConfiguration(builder.Configuration, new BootstrapAdminOptions(), new GoogleDriveOptions());
var googleDrive = configurations.GetRequired<GoogleDriveOptions>();
if (!GoogleDriveFileStorage.HasServiceAccountCredentials(googleDrive) && !GoogleDriveFileStorage.HasOAuthCredentials(googleDrive)) throw new InvalidOperationException("Google Drive credentials are missing. Set service account credentials or Google Drive OAuth credentials.");

builder.Logging.AddRadixalBpcLogging();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2 * 1024 * 1024);
builder.Services.AddDbContext<BcgHubDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<BcgHubRepository>();
builder.Services.AddScoped<IOrderReadRepository>(services => services.GetRequiredService<BcgHubRepository>());
builder.Services.AddScoped<IOrderWriteRepository>(services => services.GetRequiredService<BcgHubRepository>());
builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();
builder.Services.AddScoped<IOrderCommandService, OrderCommandService>();
builder.Services.AddSingleton<IPohodaOrderXmlParser, PohodaOrderXmlParser>();
builder.Services.AddScoped<IPohodaImportRepository>(services => services.GetRequiredService<BcgHubRepository>());
builder.Services.AddScoped<IPohodaOrderImportService, PohodaOrderImportService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IFileStorage, GoogleDriveFileStorage>();
builder.Services.AddScoped<IEntityResourceService, EntityResourceService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddSingleton<IUserPasswordGenerator, UserPasswordGenerator>();
builder.Services.AddHttpContextAccessor();
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionPath)) throw new InvalidOperationException("DataProtection:KeysPath is required.");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.IsPathRooted(dataProtectionPath) ? dataProtectionPath : Path.Combine(builder.Environment.ContentRootPath, dataProtectionPath)));
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<ICurrentOperationUserAccessor>(services => services.GetRequiredService<CurrentUserAccessor>());
builder.Services.AddScoped<EmailSettingsService>();
builder.Services.AddScoped<IEmailSettingsService>(services => services.GetRequiredService<EmailSettingsService>());
builder.Services.Configure<MicrosoftGraphOptions>(builder.Configuration.GetSection("MicrosoftGraph"));
builder.Services.AddHttpClient("MicrosoftGraph", client => client.Timeout = TimeSpan.FromSeconds(60));
builder.Services.AddScoped<MicrosoftGraphConnectionService>();
builder.Services.AddScoped<IMicrosoftGraphConnectionService>(services => services.GetRequiredService<MicrosoftGraphConnectionService>());
builder.Services.AddScoped<IMicrosoftGraphMailService, MicrosoftGraphMailService>();
builder.Services.AddScoped<IEmailQueryService, EmailQueryService>();
builder.Services.AddScoped<IEmailCommandService, EmailCommandService>();
builder.Services.AddScoped<IEmailSenderResolver, EmailSenderResolver>();
builder.Services.AddScoped<IEmailProcessor, EmailProcessor>();
builder.Services.AddScoped<IEmailTransportQuoteService, EmailTransportQuoteService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<ITransportInquiryService, TransportInquiryService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<EmailSyncService>();
builder.Services.AddScoped<IEmailSyncService>(services => services.GetRequiredService<EmailSyncService>());
builder.Services.AddSingleton<IEmailSyncLock, PostgresEmailSyncLock>();
builder.Services.AddHostedService<EmailSyncBackgroundWorker>();
builder.Services.AddScoped<UserAccountSeeder>();
builder.Services.AddSingleton<IPasswordHasher<UserAccount>, RadixalUserPasswordHasher>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "bcg-hub.session";
    options.Cookie.Path = string.IsNullOrWhiteSpace(pathBase) ? "/" : pathBase;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("Superadmin", policy => policy.RequireClaim(ClaimTypes.Email, "petr.slepicka@radixal.net"));
});
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "bcg-hub.csrf";
    options.Cookie.Path = string.IsNullOrWhiteSpace(pathBase) ? "/" : pathBase;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options => options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 })));
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRadixalBpcSwagger(options => options.Title = "BCG HUB API V1");
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("ui", policy => policy.SetIsOriginAllowed(origin => CorsOriginPolicy.IsAllowed(origin, configuredCorsOrigins)).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();
if (!string.IsNullOrWhiteSpace(pathBase)) app.UsePathBase(pathBase);
if (!app.Environment.IsDevelopment()) { app.UseHsts(); }
app.UseExceptionHandler(error => error.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("UnhandledException");
    var (status, code, message) = exception switch
    {
        DomainValidationException validation => (StatusCodes.Status400BadRequest, "validation_error", validation.Message),
        ConcurrencyConflictException conflict => (StatusCodes.Status409Conflict, "concurrency_conflict", conflict.Message),
        UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "forbidden", "K této položce nemáte přístup."),
        AntiforgeryValidationException => (StatusCodes.Status400BadRequest, "invalid_csrf_token", "Bezpečnostní token je neplatný nebo vypršel."),
        _ => (StatusCodes.Status500InternalServerError, "unexpected_error", "Při zpracování požadavku došlo k chybě.")
    };
    if (status == StatusCodes.Status500InternalServerError) logger.LogError(exception, "Unhandled request exception. TraceId: {TraceId}", context.TraceIdentifier);
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new { code, message, traceId = context.TraceIdentifier });
}));
app.UseHttpsRedirection();
app.UseRadixalBpcSwagger();
app.UseCors("ui");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<OperationLoggingMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", async (BcgHubDbContext db, CancellationToken cancellationToken) => await db.Database.CanConnectAsync(cancellationToken) ? Results.Ok(new { status = "ok", service = "BCG HUB API" }) : Results.StatusCode(StatusCodes.Status503ServiceUnavailable)).AllowAnonymous();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BcgHubDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<UserAccountSeeder>().SeedAsync();
}

app.Run();

public partial class Program;
