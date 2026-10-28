# Stop the native Qt6 beta fork gracefully so hidden banner can be restored.
$base=Split-Path -Parent $MyInvocation.MyCommand.Path
& python (Join-Path $base 'NativeStop.py')
