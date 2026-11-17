# PreToolUse hook for Edit/Write: blocks local clock reads in C# and TypeScript files.
$payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
$extension = [IO.Path]::GetExtension($payload.tool_input.file_path)
$newText = "$($payload.tool_input.content)`n$($payload.tool_input.new_string)"

$localClockPattern = switch ($extension) {
    '.cs'  { '\b(DateTime|DateTimeOffset)\.(Now|UtcNow)\b|\bTimeProvider\.System\b' }
    '.ts'  { '\bDate\.now\s*\(|\bnew\s+Date\s*\(\s*\)' }
    '.tsx' { '\bDate\.now\s*\(|\bnew\s+Date\s*\(\s*\)' }
}

if ($localClockPattern -and $newText -match $localClockPattern) {
    [Console]::Error.WriteLine("Clock event time must come from the external time API. See CLAUDE.md.")
    exit 2
}
exit 0
