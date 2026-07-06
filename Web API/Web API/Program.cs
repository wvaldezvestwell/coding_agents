using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var tvp = options.TokenValidationParameters;
        tvp.ValidateIssuer = true;
        tvp.ValidateAudience = true;
        tvp.ValidateLifetime = true;
        tvp.ValidateIssuerSigningKey = true;

        // Merge the app's Jwt section on top of anything bound from
        // Authentication:Schemes:Bearer (e.g. dotnet user-jwts in Development),
        // so tokens from either source validate.
        var jwt = builder.Configuration.GetSection("Jwt");
        if (jwt["Issuer"] is { Length: > 0 } issuer)
            tvp.ValidIssuers = [.. tvp.ValidIssuers ?? [], issuer];
        if (jwt["Audience"] is { Length: > 0 } audience)
            tvp.ValidAudiences = [.. tvp.ValidAudiences ?? [], audience];
        if (jwt["Key"] is { Length: > 0 } key)
            tvp.IssuerSigningKeys =
                [.. tvp.IssuerSigningKeys ?? [], new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))];
    });

// Secure by default: every endpoint requires an authenticated user unless it
// explicitly opts out with AllowAnonymous.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast")
    .RequireAuthorization();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
