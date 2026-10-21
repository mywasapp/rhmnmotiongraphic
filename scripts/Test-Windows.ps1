param([string]$AppDirectory = 'artifacts/portable')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
$appPath = [System.IO.Path]::GetFullPath((Join-Path $root $AppDirectory))
$exe = Join-Path $appPath 'RHMN Motion Graphic Studio.exe'
if (!(Test-Path $exe)) { throw 'Published application not found.' }
$output = Join-Path $root ('artifacts/smoke-' + [Guid]::NewGuid().ToString('N'))
New-Item $output -ItemType Directory -Force | Out-Null
$process = Start-Process $exe -ArgumentList '--smoke-test',('"' + $output + '"') -PassThru
$deadline = [DateTime]::UtcNow.AddMinutes(20)
while (!$process.WaitForExit(10000)) {
    if ([DateTime]::UtcNow -gt $deadline) { Stop-Process -Id $process.Id -Force; throw 'Windows render test timed out after 20 minutes.' }
    $progressLog = Join-Path $output 'progress.log'
    if (Test-Path $progressLog) { Get-Content $progressLog -Tail 1 | Write-Host }
}
$process.Refresh()
if ($process.ExitCode -ne 0) {
    if (Test-Path (Join-Path $output 'failure.txt')) { Get-Content (Join-Path $output 'failure.txt') | Write-Host }
    throw "Windows render test failed: exit $($process.ExitCode)"
}
$report = Get-Content (Join-Path $output 'smoke-results.json') -Raw | ConvertFrom-Json
if ($report.exitCode -ne 0 -or @($report.results | Where-Object status -eq 'passed').Count -ne 5) { throw 'Smoke report is incomplete.' }
$ffmpeg = Join-Path $appPath 'tools/ffmpeg/ffmpeg.exe'
foreach ($video in Get-ChildItem $output -Filter '*.mp4') {
    $frames = & $ffmpeg -v error -i $video.FullName -vf 'fps=1,scale=64:36' -f framemd5 -
    if ($LASTEXITCODE -ne 0) { throw 'Unable to sample rendered video.' }
    $hashes = @($frames | Where-Object { $_ -notmatch '^#' -and $_.Trim().Length -gt 0 } | ForEach-Object { ($_ -split ',')[-1].Trim() } | Select-Object -Unique)
    if ($hashes.Count -lt 2) { throw "Video appears frozen: $($video.Name)" }
}
$verifiedFiles = @(Get-ChildItem $appPath -Recurse -File | ForEach-Object { @{path=[System.IO.Path]::GetRelativePath($appPath,$_.FullName);sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash} })
$report | Add-Member -NotePropertyName verifiedFiles -NotePropertyValue $verifiedFiles
$report | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $root 'artifacts/windows-verified.json') -Encoding utf8
Write-Host "Windows integration tests passed: $output"
