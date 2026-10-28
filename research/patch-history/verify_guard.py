import ctypes, subprocess
from ctypes import wintypes as W
pidset=set()
out=subprocess.check_output(["powershell","-NoProfile","-Command","(Get-Process KakaoTalkAdGuard-Qt6Beta -ErrorAction SilentlyContinue).Id"],text=True)
pidset={int(x) for x in out.split() if x.isdigit()}
u=ctypes.windll.user32;CB=ctypes.WINFUNCTYPE(W.BOOL,W.HWND,W.LPARAM);u.EnumWindows.argtypes=[CB,W.LPARAM]
@CB
def cb(h,lp):
 pid=W.DWORD();u.GetWindowThreadProcessId(h,ctypes.byref(pid))
 if pid.value not in pidset:return True
 klass=ctypes.create_unicode_buffer(120);title=ctypes.create_unicode_buffer(120)
 u.GetClassNameW(h,klass,120);u.GetWindowTextW(h,title,120)
 print("h",int(h),"class",repr(klass.value),"title",repr(title.value),"vis",bool(u.IsWindowVisible(h)))
 return True
print('guard pid',pidset)
u.EnumWindows(cb,0)
