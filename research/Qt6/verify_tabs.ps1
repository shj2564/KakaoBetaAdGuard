Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;using System.Runtime.InteropServices;
public static class TabTester {
 [StructLayout(LayoutKind.Sequential)]public struct Pt{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]public struct R{public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out R r);
 [DllImport("user32.dll")] public static extern bool GetCursorPos(out Pt pt);
 [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] public static extern void mouse_event(uint flags,uint dx,uint dy,uint data,UIntPtr extra);
}
'@
$h=(Get-Process KakaoTalkUI).MainWindowHandle
$r=New-Object TabTester+R
[TabTester]::GetWindowRect($h,[ref]$r)|Out-Null
$cursor=New-Object TabTester+Pt
[TabTester]::GetCursorPos([ref]$cursor)|Out-Null
function ClickOffset([int]$x,[int]$y){
 [TabTester]::SetCursorPos(($r.Left+$x),($r.Top+$y))|Out-Null
 Start-Sleep -Milliseconds 80
 [TabTester]::mouse_event(2,0,0,0,[UIntPtr]::Zero)
 Start-Sleep -Milliseconds 75
 [TabTester]::mouse_event(4,0,0,0,[UIntPtr]::Zero)
 Start-Sleep -Milliseconds 470
}
function Report([string]$tag){
 Write-Output ('===='+$tag)
 $el=[System.Windows.Automation.AutomationElement]::FromHandle($h)
 $a=$el.FindAll([System.Windows.Automation.TreeScope]::Descendants,[System.Windows.Automation.Condition]::TrueCondition)
 foreach($o in $a){if($o.Current.ClassName -match '^StackView|^ContactList|^ChatRoomList'){
   Write-Output ($o.Current.ClassName+'  HEIGHT='+[int]$o.Current.BoundingRectangle.Height)
 }}
}
try {
 Report 'CHAT-INITIAL'
 ClickOffset 34 54
 Report 'FRIEND-TAB'
 ClickOffset 34 122
 Report 'CHAT-RESTORED'
}finally{
 [TabTester]::SetCursorPos($cursor.X,$cursor.Y)|Out-Null
}
