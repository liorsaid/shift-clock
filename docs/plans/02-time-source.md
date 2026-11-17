# 02 · Time source

## Goal
One component that asks TimeAPI.io for the current Zurich time, and either returns a time it trusts or fails. Clock in/out uses it from step 6.

## What's built
- ITimeSource: GetZurichNowAsync returns a DateTimeOffset with the Zurich offset.
- TimeApiTimeSource: calls TimeAPI.io through a typed HttpClient.
- TimeSourceUnavailableException: the one error for every failure. Step 6 maps it to 503.
- TimeApiOptions: the URL and the 3s timeout, from appsettings.json, checked at startup.
- Tests: StubHttpMessageHandler (a fake network), FakeTimeSource (for step 6) and TimeApiTimeSourceTests.

## How it works
A response is accepted only if:
1. The HTTP call succeeds within 3 seconds.
2. The JSON is valid and has date_time, timezone and utc_offset_seconds.
3. timezone is exactly Europe/Zurich.
4. date_time ends with an explicit offset. Without one, .NET would fill in the machine's offset.
5. The offset matches utc_offset_seconds.
6. The offset matches Zurich's rules at that instant (time zone rules, not the clock).

Anything else throws TimeSourceUnavailableException, and the reason is logged. A cancellation by the caller is not a failure.

## Tests
Built on a real TimeAPI.io response. The real API is never called.
- Accepted: the real response, times at each daylight saving change, and both 02:30s of the autumn night.
- Rejected: each check above failing, a timeout, an unknown charset, and the skipped spring hour.
- The configured URL is called once, and a caller cancellation stays a cancellation.
