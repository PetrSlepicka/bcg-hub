using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BcgHub.Api.Application;
using BcgHub.Api.Domain;
using BcgHub.Api.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Connection string 'DefaultConnection' is required. Configure it through secrets or environment variables.");

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 2 * 1024 * 1024);
builder.Services.Configure<BootstrapAdminOptions>(builder.Configuration.GetSection("BootstrapAdmin"));
builder.Services.AddDbContext<BcgHubDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<BcgHubRepository>();
builder.Services.AddScoped<IOrderReadRepository>(services => services.GetRequiredService<BcgHubRepository>());
builder.Services.AddScoped<IOrderWriteRepository>(services => services.GetRequiredService<BcgHubRepository>());
builder.Services.AddScoped<IOrderQueryService, OrderQueryService>();
builder.Services.AddScoped<IOrderCommandService, OrderCommandService>();
builder.Services.AddScoped<IPartnerQueryService, PartnerQueryService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (string.IsNullOrWhiteSpace(dataProtectionPath)) throw new InvalidOperationException("DataProtection:KeysPath is required.");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.IsPathRooted(dataProtectionPath) ? dataProtectionPath : Path.Combine(builder.Environment.ContentRootPath, dataProtectionPath)));
builder.Services.AddScoped<CurrentUserAccessor>();
builder.Services.AddScoped<EmailSettingsService>();
builder.Services.AddScoped<IEmailSettingsService>(services => services.GetRequiredService<EmailSettingsService>());
builder.Services.AddScoped<IEmailQueryService, EmailQueryService>();
builder.Services.AddScoped<IEmailCommandService, EmailCommandService>();
builder.Services.AddScoped<IEmailSyncService, EmailSyncService>();
builder.Services.AddSingleton<IEmailSyncLock, PostgresEmailSyncLock>();
builder.Services.AddScoped<UserAccountSeeder>();
builder.Services.AddSingleton<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "bcg-hub.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(12);
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "bcg-hub.csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options => options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 })));
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddCors(options => options.AddPolicy("ui", policy => policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? []).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseHsts(); }
app.UseExceptionHandler(error => error.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("UnhandledException");
    var (status, code, message) = exception switch
    {
        DomainValidationException validation => (StatusCodes.Status400BadRequest, "validation_error", validation.Message),
        ConcurrencyConflictException conflict => (StatusCodes.Status409Conflict, "concurrency_conflict", conflict.Message),
        AntiforgeryValidationException => (StatusCodes.Status400BadRequest, "invalid_csrf_token", "Bezpečnostní token je neplatný nebo vypršel."),
        _ => (StatusCodes.Status500InternalServerError, "unexpected_error", "Při zpracování požadavku došlo k chybě.")
    };
    if (status == StatusCodes.Status500InternalServerError) logger.LogError(exception, "Unhandled request exception. TraceId: {TraceId}", context.TraceIdentifier);
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new { code, message, traceId = context.TraceIdentifier });
}));
app.UseHttpsRedirection();
app.UseCors("ui");
app.UseRateLimiter();
app.UseAuthentication();
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
