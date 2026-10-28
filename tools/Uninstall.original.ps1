# Uninstall only the user's KakaoTalkAdGuard Qt6 beta fork autostart.
# Does not change original KakaoTalk installation, user data, or network.
$ErrorActionPreference='Stop'
$folder=Split-Path -Parent $MyInvocation.MyCommand.Path
& (Join-Path $folder 'StopGuard.ps1')
$key='HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$expected='"'+(Join-Path $folder 'KakaoTalkAdGuard-Qt6Beta.exe')+'"'
$entry=Get-ItemPropertyValue -Path $key -Name 'SoNG-KakaoBetaAdGuard' -ErrorAction SilentlyContinue
if($entry -eq $expected){
  Remove-ItemProperty -Path $key -Name 'SoNG-KakaoBetaAdGuard'
  Write-Output 'Removed SoNG KakaoTalk Qt6 beta guard startup.'
}else{
  Write-Output 'Startup value has changed or is absent; leaving it unchanged.'
}
Write-Output 'The native program and its source files remain available in this folder.'
