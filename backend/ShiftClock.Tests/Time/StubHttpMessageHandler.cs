using System.Diagnostics;
using System.Net;

namespace ShiftClock.Tests.Time;

public sealed class StubHttpMessageHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public static StubHttpMessageHandler Returning(HttpStatusCode statusCode, string body) =>
        new(_ => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body) }));

    public static StubHttpMessageHandler Throwing(Exception exception) =>
        new(_ => Task.FromException<HttpResponseMessage>(exception));

    public static StubHttpMessageHandler NeverResponding() =>
        new(async cancellationToken =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new UnreachableException();
        });

    public List<HttpRequestMessage> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return respond(cancellationToken);
    }
}
