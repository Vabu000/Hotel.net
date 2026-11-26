using HotelPlatform.ServiceDefaults.Authorization;
using HotelPlatform.ServiceDefaults.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddPlatformAuthentication(
    builder.Configuration,
    builder.Environment,
    identityServerAudience: "payments_api",
    keycloakAudience: "payments-api");

builder.Services.AddPermissionPolicies(
    "payments:read",
    "payments:process",
    "payments:refund");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseServiceDefaults();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
