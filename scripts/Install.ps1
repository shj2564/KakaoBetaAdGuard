# Run from the checked-out repository root: powershell -ExecutionPolicy Bypass -File .\scripts\Install.ps1
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$release=Join-Path $root 'release\windows-x64'
$destination=Join-Path $env:LOCALAPPDATA 'SoNG-KakaoBetaAdGuard'
$exe='KakaoTalkAdGuard-Qt6Beta.exe'
$dll='SoNGQt6Layout.dll'
foreach($name in @($exe,$dll)){if(-not (Test-Path (Join-Path $release $name))){throw "Missing release file: $name"}}
& (Join-Path $PSScriptRoot 'StopGuard.ps1')
Start-Sleep -Milliseconds 500
if(Get-Process -Name 'KakaoTalkAdGuard-Qt6Beta' -ErrorAction SilentlyContinue){throw 'Guard still running. Exit via the tray and retry.'}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach($name in @($exe,$dll)){Copy-Item (Join-Path $release $name) $destination -Force}
$icon=Join-Path $release 'SoNG-KakaoBetaAdGuard.ico'
if(Test-Path $icon){Copy-Item $icon $destination -Force}
$entry='"'+(Join-Path $destination $exe)+'"'
New-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'SoNG-KakaoBetaAdGuard' -Value $entry -PropertyType String -Force | Out-Null
Start-Process (Join-Path $destination $exe)
Write-Output "Installed and registered for current-user login: $destination"
