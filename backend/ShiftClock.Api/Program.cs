using Microsoft.Extensions.Options;
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
    client.Timeout = services.GetRequiredService<IOptions<TimeApiOptions>>().Value.Timeout);

var app = builder.Build();

app.Run();
