using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using HotelAuth.IdentityServer;
using HotelAuth.IdentityServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("IdentityServerDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityServerDb' must be supplied by configuration.");
var configurationConnectionString = builder.Configuration.GetConnectionString("IdentityServerConfigDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityServerConfigDb' must be supplied by configuration.");
var grantsConnectionString = builder.Configuration.GetConnectionString("IdentityServerGrantsDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityServerGrantsDb' must be supplied by configuration.");
var aggregatorClientSecret = builder.Configuration["IdentityServer:Clients:AggregatorSecret"];
if (string.IsNullOrWhiteSpace(aggregatorClientSecret))
{
    throw new InvalidOperationException("IdentityServer:Clients:AggregatorSecret must be supplied as a secret.");
}
// ASP.NET Core Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Duende IdentityServer
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
    options.EmitStaticAudienceClaim = true;
})
.AddConfigurationStore(options =>
{
    options.ConfigureDbContext = b => b.UseNpgsql(configurationConnectionString);
})
.AddOperationalStore(options =>
{
    options.ConfigureDbContext = b => b.UseNpgsql(grantsConnectionString);
    options.EnableTokenCleanup = true;
    options.TokenCleanupInterval = 3600;
})
.AddAspNetIdentity<ApplicationUser>();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();

var app = builder.Build();
app.UseServiceDefaults();

// Initialize database with seed data
await InitializeDatabaseAsync(app, aggregatorClientSecret);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();

static async Task InitializeDatabaseAsync(WebApplication app, string aggregatorClientSecret)
{
    using var scope = app.Services.CreateScope();

    var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
    var persistedGrantContext = scope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>();
    var appContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await configContext.Database.EnsureCreatedAsync();
    await persistedGrantContext.Database.EnsureCreatedAsync();
    await appContext.Database.EnsureCreatedAsync();

    // Seed IdentityServer config
    if (!configContext.Clients.Any())
    {
        foreach (var client in Config.GetClients(aggregatorClientSecret))
            configContext.Clients.Add(client.ToEntity());
        await configContext.SaveChangesAsync();
    }

    if (!configContext.IdentityResources.Any())
    {
        foreach (var resource in Config.IdentityResources)
            configContext.IdentityResources.Add(resource.ToEntity());
        await configContext.SaveChangesAsync();
    }

    if (!configContext.ApiScopes.Any())
    {
        foreach (var scope2 in Config.ApiScopes)
            configContext.ApiScopes.Add(scope2.ToEntity());
        await configContext.SaveChangesAsync();
    }

    if (!configContext.ApiResources.Any())
    {
        foreach (var resource in Config.ApiResources)
            configContext.ApiResources.Add(resource.ToEntity());
        await configContext.SaveChangesAsync();
    }

    // Seed admin user
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Manager", "Guest" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = app.Configuration["IdentityServer:SeedAdmin:Email"];
    var adminPassword = app.Configuration["IdentityServer:SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail)
        && !string.IsNullOrWhiteSpace(adminPassword)
        && await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Hotel Administrator",
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Configured IdentityServer admin could not be created: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
        }

        await userManager.AddToRoleAsync(admin, "Admin");
    }
}
