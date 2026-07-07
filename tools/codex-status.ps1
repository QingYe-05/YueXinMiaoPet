param(
    [string]$Status = "idle",
    [string]$Title = "",
    [string]$Message = "",
    [string]$Task = "",
    [int]$Progress = 0,
    [string]$Source = "manual",
    [string]$Path = ""
)

function Escape-JsonString([string]$Value) {
    if ($null -eq $Value) {
        return ""
    }

    $builder = New-Object System.Text.StringBuilder
    foreach ($ch in $Value.ToCharArray()) {
        $code = [int][char]$ch
        if ($code -eq 34) {
            [void]$builder.Append('\"')
        }
        elseif ($code -eq 92) {
            [void]$builder.Append('\\')
        }
        elseif ($code -eq 8) {
            [void]$builder.Append('\b')
        }
        elseif ($code -eq 9) {
            [void]$builder.Append('\t')
        }
        elseif ($code -eq 10) {
            [void]$builder.Append('\n')
        }
        elseif ($code -eq 12) {
            [void]$builder.Append('\f')
        }
        elseif ($code -eq 13) {
            [void]$builder.Append('\r')
        }
        elseif ($code -lt 32 -or $code -gt 126) {
            [void]$builder.Append('\u')
            [void]$builder.Append($code.ToString('x4'))
        }
        else {
            [void]$builder.Append($ch)
        }
    }

    return $builder.ToString()
}

function Get-StatusTitle([string]$StatusValue) {
    switch ($StatusValue.ToLowerInvariant()) {
        "idle" { return "Codex idle" }
        "planning" { return "Codex planning" }
        "reading" { return "Codex reading" }
        "coding" { return "Codex coding" }
        "building" { return "Codex building" }
        "testing" { return "Codex testing" }
        "reviewing" { return "Codex reviewing" }
        "waiting" { return "Codex waiting" }
        "done" { return "Codex done" }
        "error" { return "Codex error" }
        default { return "Codex unknown" }
    }
}

$allowed = @("idle", "planning", "reading", "coding", "building", "testing", "reviewing", "waiting", "done", "error")
$normalizedStatus = $Status.ToLowerInvariant()
if ($allowed -notcontains $normalizedStatus) {
    $normalizedStatus = "idle"
}

if ([string]::IsNullOrWhiteSpace($Path)) {
    $Path = Join-Path $env:APPDATA "YueXinMiaoPet\codex_status.json"
}

if ([string]::IsNullOrWhiteSpace($Title)) {
    $Title = Get-StatusTitle $normalizedStatus
}

if ($Progress -lt 0) { $Progress = 0 }
if ($Progress -gt 100) { $Progress = 100 }

$directory = Split-Path -Parent $Path
if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$updatedAt = [System.DateTimeOffset]::Now.ToString("o")
$json = @"
{
  "enabled": true,
  "status": "$(Escape-JsonString $normalizedStatus)",
  "title": "$(Escape-JsonString $Title)",
  "message": "$(Escape-JsonString $Message)",
  "task": "$(Escape-JsonString $Task)",
  "progress": $Progress,
  "updatedAt": "$(Escape-JsonString $updatedAt)",
  "source": "$(Escape-JsonString $Source)"
}
"@

$encoding = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($Path, $json, $encoding)
Write-Host "Codex status written to: $Path"
