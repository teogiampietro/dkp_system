using DkpSystem.Components;
using DkpSystem.Data;
using DkpSystem.Data.Identity;
using DkpSystem.Data.Repositories;
using DkpSystem.Models;
using DkpSystem.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure Circuit options for detailed errors
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options => { options.DetailedErrors = true; });

// Register DbConnectionFactory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddSingleton(new DbConnectionFactory(connectionString));

// Register repositories
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<MemberRepository>();

// Register custom Identity store
builder.Services.AddScoped<IUserStore<User>, DapperUserStore>();

// Add authentication services
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Configure ASP.NET Core Identity (using IdentityCore since we don't need role store)
builder.Services.AddIdentityCore<User>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        // User settings
        options.User.RequireUniqueEmail = true;

        // Sign-in settings
        options.SignIn.RequireConfirmedEmail = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddDefaultTokenProviders()
    .AddSignInManager<SignInManager<User>>()
    .AddClaimsPrincipalFactory<CustomUserClaimsPrincipalFactory>();

// Configure authentication cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/unauthorized";
    options.SlidingExpiration = true;
});

// Register authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

// Register application services
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<MemberService>();

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("RaiderOrAdmin", policy => policy.RequireRole("raider", "admin"));
});

// Add cascading authentication state
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Log current environment
var environment = app.Environment.EnvironmentName;
app.Logger.LogInformation("🚀 Starting application in {Environment} environment", environment);
app.Logger.LogInformation("📁 Using configuration from appsettings.json" +
                          (environment != "Production" ? $" + appsettings.{environment}.json" : ""));

// Run database migrations on startup
try
{
    var migrator = new DatabaseMigrator(connectionString);
    await migrator.RunMigrationsAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️  Migration warning: {ex.Message}");
    Console.WriteLine("Continuing with application startup...");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map logout endpoint
app.MapPost("/logout", async (AuthenticationService authService, HttpContext context) =>
{
    await authService.LogoutAsync();
    context.Response.Redirect("/");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();