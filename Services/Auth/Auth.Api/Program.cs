using System.Text;
using System.Net.Mail;
using HotelAuth.Api.Data;
using HotelAuth.Api.Models;
using HotelAuth.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("IdentityDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityDb' must be supplied by configuration.");

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.SecretKey) || System.Text.Encoding.UTF8.GetByteCount(jwt.SecretKey) < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey must be supplied as a secret and contain at least 32 bytes.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Issuer,
        ValidateAudience = true,
        ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
var smtpOptions = builder.Services.AddOptions<SmtpEmailOptions>()
    .Bind(builder.Configuration.GetSection("Smtp"));
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailService, MockEmailService>();
}
else
{
    smtpOptions
        .Validate(options => !string.IsNullOrWhiteSpace(options.Host)
            && options.Port is > 0 and <= 65535
            && MailAddress.TryCreate(options.FromAddress, out _)
            && (string.IsNullOrWhiteSpace(options.Username) == string.IsNullOrWhiteSpace(options.Password)),
            "SMTP host, a valid sender address, and matching optional credentials must be configured.")
        .ValidateOnStart();
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseServiceDefaults();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "User", "Admin" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminEmail = builder.Configuration["Auth:SeedAdmin:Email"];
    var adminPassword = builder.Configuration["Auth:SeedAdmin:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword)
        && await userManager.FindByEmailAsync(adminEmail) is null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Platform Administrator",
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(admin, adminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Configured admin could not be created: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
        }

        await userManager.AddToRolesAsync(admin, ["Admin", "User"]);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
