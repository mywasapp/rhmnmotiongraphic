param([string]$Version = '0.1.0')
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
if (!(Test-Path 'artifacts/windows-verified.json')) { throw 'Packaging blocked: a successful Windows render test is required.' }
$verification = Get-Content 'artifacts/windows-verified.json' -Raw | ConvertFrom-Json
if ($verification.exitCode -ne 0) { throw 'Windows verification did not pass.' }
$portable = Join-Path $root 'artifacts/portable'
if (!$verification.verifiedFiles) { throw 'Windows test evidence has no payload hashes.' }
foreach ($verified in $verification.verifiedFiles) {
    $file = Join-Path $portable $verified.path
    if (!(Test-Path $file) -or (Get-FileHash $file -Algorithm SHA256).Hash -ne $verified.sha256) { throw "Tested payload changed: $($verified.path). Run Windows tests again." }
}
Copy-Item 'artifacts/dependencies/MicrosoftEdgeWebview2Setup.exe' $portable -Force
Copy-Item 'artifacts/dependencies/dependencies.json' $portable -Force
Copy-Item 'artifacts/windows-verified.json' $portable -Force
Copy-Item 'docs/PANDUAN.md' (Join-Path $portable 'PANDUAN.md') -Force
$exe = Join-Path $portable 'RHMN Motion Graphic Studio.exe'
$bytes = [System.IO.File]::ReadAllBytes($exe)
if ($bytes[0] -ne 0x4D -or $bytes[1] -ne 0x5A) { throw 'App is not a Windows executable.' }
$pe = [BitConverter]::ToInt32($bytes,0x3C)
if ([BitConverter]::ToUInt16($bytes,$pe+4) -ne 0x8664) { throw 'App is not Windows x64.' }
$manifest = Get-ChildItem $portable -Recurse -File | ForEach-Object { @{path=[System.IO.Path]::GetRelativePath($portable,$_.FullName);sha256=(Get-FileHash $_.FullName -Algorithm SHA256).Hash;bytes=$_.Length} }
@{version=$Version;commit=$env:GITHUB_SHA;run=$env:GITHUB_RUN_ID;windows_verified=$true;files=$manifest} | ConvertTo-Json -Depth 6 | Set-Content 'artifacts/build-manifest.json' -Encoding utf8
Copy-Item 'artifacts/build-manifest.json' $portable -Force
Compress-Archive -Path "$portable/*" -DestinationPath "artifacts/RHMN-Motion-Graphic-Studio-$Version-win-x64-Portable.zip" -CompressionLevel Optimal -Force
$iscc = Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'
if (!(Test-Path $iscc)) { throw 'Inno Setup compiler is not installed.' }
& $iscc "/DMyAppVersion=$Version" 'installer/RHMN.iss'
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$installer = Join-Path $root "artifacts/RHMN-Motion-Graphic-Studio-$Version-win-x64-Setup.exe"
if (!(Test-Path $installer)) { throw 'Installer output missing.' }
$temporaryRoot = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { $env:TEMP }
$installDir = Join-Path $temporaryRoot ('RHMN-install-test-' + [Guid]::NewGuid().ToString('N'))
$install = Start-Process $installer -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART',('/DIR="'+$installDir+'"') -PassThru -Wait
if ($install.ExitCode -ne 0 -or !(Test-Path (Join-Path $installDir 'RHMN Motion Graphic Studio.exe'))) { throw 'Installer installation test failed.' }
if ((Get-FileHash (Join-Path $installDir 'RHMN Motion Graphic Studio.exe')).Hash -ne (Get-FileHash $exe).Hash) { throw 'Installed application does not match the tested executable.' }
foreach ($verified in $verification.verifiedFiles) {
    if ((Get-FileHash (Join-Path $installDir $verified.path) -Algorithm SHA256).Hash -ne $verified.sha256) { throw "Installer payload differs: $($verified.path)" }
}
Get-ChildItem 'artifacts/RHMN-*.zip','artifacts/RHMN-*.exe' | ForEach-Object { "$((Get-FileHash $_.FullName -Algorithm SHA256).Hash)  $($_.Name)" } | Set-Content 'artifacts/SHA256SUMS.txt' -Encoding utf8
