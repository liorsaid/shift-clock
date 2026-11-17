using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShiftClock.Api.Data;
using ShiftClock.Api.SignIn;
using ShiftClock.Api.Time;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<TimeApiOptions>()
    .Bind(builder.Configuration.GetSection(TimeApiOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(options.Url, UriKind.Absolute, out var url) && url.Scheme == Uri.UriSchemeHttps,
        "TimeApi:Url must be an absolute https URL.")
    .Validate(options => options.Timeout > TimeSpan.Zero, "TimeApi:Timeout must be positive.")
    .ValidateOnStart();
builder.Services.AddHttpClient<ITimeSource, TimeApiTimeSource>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<TimeApiOptions>>().Value;
    client.Timeout = options.Timeout;
});
builder.Services.AddDbContext<ShiftClockDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ShiftClock")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ShiftClock.Session";
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ShiftClockDbContext>();
    db.Database.Migrate();
    DemoEmployees.AddIfEmpty(db);
}

app.MapSignInEndpoints();

app.Run();
