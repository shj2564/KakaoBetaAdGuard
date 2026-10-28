import ctypes, time
user32=ctypes.windll.user32
h=user32.FindWindowW("KAKAOTALKADGUARD",None)
if h:
    ok=user32.PostMessageW(h,0x0010,0,0)
    time.sleep(0.3)
    print("Graceful KakaoTalkAdGuard Qt6 beta stop requested:",bool(ok))
else:
    print("KakaoTalkAdGuard Qt6 beta main window not found; nothing changed")
