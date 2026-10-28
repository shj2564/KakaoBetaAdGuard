# Gracefully stop the native Qt6 helper (allows KakaoTalk layout restoration).
$ErrorActionPreference='Stop'
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SoNGGuardControl {
 [DllImport("user32.dll", EntryPoint="FindWindowW", CharSet=CharSet.Unicode)]
 public static extern IntPtr FindWindow(string name,string title);
 [DllImport("user32.dll", EntryPoint="PostMessageW")]
 public static extern bool PostMessage(IntPtr h,uint m,IntPtr wp,IntPtr lp);
}
'@
$h=[SoNGGuardControl]::FindWindow('KAKAOTALKADGUARD',$null)
if($h -eq [IntPtr]::Zero){Write-Output 'No running guard main window found';return}
if(-not [SoNGGuardControl]::PostMessage($h,0x10,[IntPtr]::Zero,[IntPtr]::Zero)){throw 'Could not send WM_CLOSE'}
Write-Output 'Graceful stop requested. Qt6 layout restoration is handled by the guard.'
