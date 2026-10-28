from pathlib import Path
p=Path(r'C:\Users\SoNG-MaIN\AppData\Local\SoNG-KakaoBetaAdGuard\KakaoTalkAdGuard_BetaFork\KakaoTalkAdGuard\main.cpp')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('BOOL             CreateTrayIcon(HWND hWnd, NOTIFYICONDATA* nid);',
'''VOID             RestoreBetaBanner();
BOOL             CreateTrayIcon(HWND hWnd, NOTIFYICONDATA* nid);''',1)
s=s.replace('''\tcase WM_DESTROY:
\t\tKillTimer(hWnd, 1);''','''\tcase WM_DESTROY:
        RestoreBetaBanner();
\t\tKillTimer(hWnd, 1);''',1)
s=s.replace('''\tbHideMainPannelAd = not regHideMainPannelAd;
\tRegSetValueExW''','''\tbHideMainPannelAd = not regHideMainPannelAd;
    if(!bHideMainPannelAd)RestoreBetaBanner();
\tRegSetValueExW''',1)
s=s.replace('''struct BETA_CHILD_STATE{bool chrome=false;};''','''static bool g_restoreBetaBanner=false;
struct BETA_CHILD_STATE{bool chrome=false;};''',1)
s=s.replace('''    if(IsWindowVisible(h))ShowWindow(h,SW_HIDE);
    return TRUE;
}

VOID CALLBACK TimerProc''','''    if(g_restoreBetaBanner) {
        if(!IsWindowVisible(h))ShowWindow(h,SW_SHOWNOACTIVATE);
    } else {
        if(IsWindowVisible(h))ShowWindow(h,SW_HIDE);
    }
    return TRUE;
}
VOID RestoreBetaBanner() {
    BETA_MAIN beta;
    EnumWindows(FindQtBetaMain,reinterpret_cast<LPARAM>(&beta));
    if(beta.hwnd) {
        g_restoreBetaBanner=true;
        EnumWindows(HideQtBetaAd,reinterpret_cast<LPARAM>(&beta));
        g_restoreBetaBanner=false;
    }
}
VOID CALLBACK TimerProc''',1)
assert "VOID RestoreBetaBanner() {" in s
p.write_text(s,encoding='utf-8-sig')
print('patched on-exit and checkbox ad restore; size',p.stat().st_size)
