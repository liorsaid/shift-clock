# PreToolUse hook for Edit/Write: blocks every local clock read.
# The external time API is the only source of time. Duration timers (Stopwatch, performance.now()) stay allowed.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$filePath = "$($payload.tool_input.file_path)" -replace '\\', '/'
$newText = "$($payload.tool_input.content)`n$($payload.tool_input.new_string)"

$browser = @(
    '\bDate\.now\s*\('
    '\bnew\s+Date\b(?!\s*\(\s*[^)\s])'
    '(?<![\w.]|new\s+)Date\s*\('
    '\bperformance\.timeOrigin\b'
    '\bTemporal\.Now\b'
)
$server = @(
    '\b(DateTime|DateTimeOffset)\.(Now|UtcNow|Today)\b'
    '\bTimeProvider\.System\b'
    '\bSystemClock\b'
)
$database = @(
    '(?i)(?<![\w.])(GETDATE|GETUTCDATE|SYSDATETIME|SYSUTCDATETIME|SYSDATETIMEOFFSET)\s*\('
    '(?i)\bCURRENT_(TIMESTAMP|DATE)\b'
)

if ($filePath -like '*/.claude/*') { exit 0 }

$patterns = switch ([IO.Path]::GetExtension($filePath)) {
    { $_ -in '.ts', '.tsx', '.js', '.jsx' } { $browser }
    '.cs'  { $server + $database }
    '.sql' { $database }
}

foreach ($pattern in $patterns) {
    if ($newText -cmatch $pattern) {
        [Console]::Error.WriteLine("Clock event time must come from the external time API. See CLAUDE.md. Found: $($Matches[0].Trim())")
        exit 2
    }
}
exit 0
