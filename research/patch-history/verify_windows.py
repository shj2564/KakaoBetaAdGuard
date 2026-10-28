import ctypes, subprocess
from ctypes import wintypes as W
u=ctypes.windll.user32
class R(ctypes.Structure):
    _fields_=[("left",ctypes.c_int),("top",ctypes.c_int),("right",ctypes.c_int),("bottom",ctypes.c_int)]
CB=ctypes.WINFUNCTYPE(ctypes.c_bool,W.HWND,W.LPARAM)
u.EnumWindows.argtypes=[CB,W.LPARAM]
u.EnumChildWindows.argtypes=[W.HWND,CB,W.LPARAM]
u.GetWindowThreadProcessId.argtypes=[W.HWND,ctypes.POINTER(W.DWORD)]
u.GetClassNameW.argtypes=[W.HWND,W.LPWSTR,ctypes.c_int]
u.GetWindowTextW.argtypes=[W.HWND,W.LPWSTR,ctypes.c_int]
u.GetWindowRect.argtypes=[W.HWND,ctypes.POINTER(R)]
pids=set()
for line in subprocess.check_output(['powershell','-NoProfile','-Command',"(Get-Process KakaoTalkUI -ErrorAction SilentlyContinue).Id"],text=True).split():
    if line.isdigit():pids.add(int(line))
print("Kakao process ids", sorted(pids))
@CB
def child(h,p):
    c=ctypes.create_unicode_buffer(100)
    u.GetClassNameW(h,c,100)
    if 'Chrome_WidgetWin_' in c.value: print('   CHILD CHROME',int(h))
    return True
@CB
def top(h,p):
    pid=W.DWORD()
    u.GetWindowThreadProcessId(h,ctypes.byref(pid))
    if pid.value not in pids:return True
    c=ctypes.create_unicode_buffer(120)
    title=ctypes.create_unicode_buffer(120)
    u.GetClassNameW(h,c,120)
    u.GetWindowTextW(h,title,120)
    r=R();u.GetWindowRect(h,ctypes.byref(r))
    w=r.right-r.left;ht=r.bottom-r.top
    if w>200 or ht>80:
        print(dict(hwnd=int(h),pid=pid.value,klass=c.value,width=w,height=ht,visible=bool(u.IsWindowVisible(h)),title=('kakaotalk' if ('카카오톡' in title.value or 'KakaoTalk' in title.value) else 'omitted')))
        if 'QWindowIcon' in c.value and w>300 and ht<=130:u.EnumChildWindows(h,child,0)
    return True
u.EnumWindows(top,0)
