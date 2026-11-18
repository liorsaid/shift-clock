using System.Globalization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ShiftClock.Api.Time;
using ShiftClock.Tests.Data;
using ShiftClock.Tests.Time;

namespace ShiftClock.Tests;

public sealed class ShiftClockApp : WebApplicationFactory<Program>
{
    private const string EnvironmentWithoutDemoData = "Testing";

    public FakeTimeSource TimeSource { get; } =
        new(DateTimeOffset.Parse("2026-11-17T08:00:00+01:00", CultureInfo.InvariantCulture));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentWithoutDemoData);
        builder.UseSetting("ConnectionStrings:ShiftClock", TestDatabase.ConnectionString);
        builder.ConfigureTestServices(services => services.AddSingleton<ITimeSource>(TimeSource));
    }
}
