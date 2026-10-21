param([switch]$InstallWebView2)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = Split-Path $PSScriptRoot -Parent
$cache = Join-Path $root 'artifacts/dependencies'
$target = Join-Path $root 'vendor/ffmpeg'
New-Item $cache,$target -ItemType Directory -Force | Out-Null
$ffmpegUrl = 'https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip'
$expected = '60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba'
$zip = Join-Path $cache 'ffmpeg-9.0.2.zip'
if (!(Test-Path $zip)) { Invoke-WebRequest $ffmpegUrl -OutFile $zip }
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
if ($hash -ne $expected) { throw 'FFmpeg checksum mismatch. The upstream release may have changed. Review the new version and update the pinned hash; never skip this check.' }
$extract = Join-Path $cache 'ffmpeg-expanded'
Expand-Archive $zip -DestinationPath $extract -Force
Get-ChildItem $extract -Recurse -File | Where-Object { $_.Name -in @('ffmpeg.exe','ffprobe.exe','LICENSE','LICENSE.txt','README.txt') } | ForEach-Object { Copy-Item $_.FullName $target -Force }
foreach ($name in 'ffmpeg.exe','ffprobe.exe') { if (!(Test-Path (Join-Path $target $name))) { throw "Dependency missing: $name" } }
$bootstrap = Join-Path $cache 'MicrosoftEdgeWebview2Setup.exe'
$webviewUrl = 'https://go.microsoft.com/fwlink/p/?LinkId=2124703'
if (!(Test-Path $bootstrap)) { Invoke-WebRequest $webviewUrl -OutFile $bootstrap }
$signature = Get-AuthenticodeSignature $bootstrap
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'Microsoft Corporation') { throw 'WebView2 bootstrapper does not have a valid Microsoft signature.' }
@{ ffmpeg=@{version='9.0.2';url=$ffmpegUrl;sha256=$hash}; webview2=@{url=$webviewUrl;sha256=(Get-FileHash $bootstrap -Algorithm SHA256).Hash;signer=$signature.SignerCertificate.Subject} } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $cache 'dependencies.json') -Encoding utf8
if ($InstallWebView2) {
    $key = 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}'
    $machine = Get-ItemProperty -Path "HKLM:\Software\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" -Name pv -ErrorAction SilentlyContinue
    $user = Get-ItemProperty -Path "HKCU:\$key" -Name pv -ErrorAction SilentlyContinue
    if (!$machine -and !$user) {
        $installer = Start-Process $bootstrap -ArgumentList '/silent','/install' -PassThru -Wait
        if ($installer.ExitCode -notin @(0,3010)) { throw "WebView2 installation failed: $($installer.ExitCode)" }
    }
}
Write-Host 'Dependencies verified.'
