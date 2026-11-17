using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using ShiftClock.Tests.Data;

namespace ShiftClock.Tests;

public sealed class ShiftClockApp : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ShiftClock", TestDatabase.ConnectionString);
    }
}
