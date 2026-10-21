param([string]$AppDirectory='artifacts/portable')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$destination=Join-Path (Join-Path $root $AppDirectory) 'licenses'
New-Item $destination -ItemType Directory -Force | Out-Null
$packageRoot=if($env:NUGET_PACKAGES){$env:NUGET_PACKAGES}else{Join-Path $env:USERPROFILE '.nuget/packages'}
foreach($package in 'microsoft.netcore.app.runtime.win-x64','microsoft.windowsdesktop.app.runtime.win-x64','microsoft.web.webview2') {
  $versions=Get-ChildItem (Join-Path $packageRoot $package) -Directory | Sort-Object {[version]($_.Name -split '-')[0]} -Descending
  $latest=$versions | Select-Object -First 1
  if(!$latest){throw "No notices found for $package"}
  $folder=Join-Path $destination $package
  New-Item $folder -ItemType Directory -Force | Out-Null
  Get-ChildItem $latest.FullName -File | Where-Object {$_.Name -match '^(LICENSE|THIRD-PARTY-NOTICES)'} | ForEach-Object {Copy-Item $_.FullName $folder -Force}
}
