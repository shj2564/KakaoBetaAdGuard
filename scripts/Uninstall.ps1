# Stop and remove only our current-user startup registry value.
$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'StopGuard.ps1')
$target='"'+(Join-Path $env:LOCALAPPDATA 'SoNG-KakaoBetaAdGuard\KakaoTalkAdGuard-Qt6Beta.exe')+'"'
$key='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$actual=Get-ItemPropertyValue -Path $key -Name 'SoNG-KakaoBetaAdGuard' -ErrorAction SilentlyContinue
if($actual -eq $target){
 Remove-ItemProperty -Path $key -Name 'SoNG-KakaoBetaAdGuard'
 Write-Output 'Removed SoNG guard autorun. Installed files remain for rollback.'
}else{Write-Output 'Autorun absent or different: no changes made.'}
