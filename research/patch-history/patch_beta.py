from pathlib import Path
p=Path(r'C:\Users\SoNG-MaIN\AppData\Local\SoNG-KakaoBetaAdGuard\KakaoTalkAdGuard_BetaFork\KakaoTalkAdGuard\main.cpp')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('#include <winhttp.h>','#include <winhttp.h>\n#include <tlhelp32.h>',1)
s=s.replace('bHideMainPannelAd = (RegQueryValueExW(key, L"HideMainPannelAd", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize) == ERROR_SUCCESS && dwValue != 0);',
'''// Default to enabled on this Qt6 beta fork; preserve user's explicit setting.
    LSTATUS adOptionStatus = RegQueryValueExW(key, L"HideMainPannelAd", 0, &dwType, (LPBYTE)&dwValue, &dwDataSize);
    bHideMainPannelAd = (adOptionStatus == ERROR_SUCCESS) ? (dwValue != 0) : TRUE;''',1)
point='VOID CALLBACK TimerProc(HWND hwnd, UINT message, UINT idEvent, DWORD dwTimer) {'
assert point in s
new= r'''
// Qt6 beta compatibility. All Qt windows are checked against the actual
// KakaoTalkUI.exe process and a separate Chrome-bearing banner.
// Deliberately no outer-window resizing/SetWindowRgn/overlay: QML list items
// aren't native EVA_ChildWindow windows and cannot safely be resized by Win32.
struct BETA_MAIN {
    DWORD pid=0;
    HWND hwnd=NULL;
    RECT rect={0};
};
static bool IsBetaProcess(DWORD pid) {
    if(!pid)return false;
    HANDLE proc=OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION,FALSE,pid);
    if(!proc)return false;
    WCHAR path[MAX_PATH]={0};DWORD size=MAX_PATH;
    BOOL ok=QueryFullProcessImageNameW(proc,0,path,&size);
    CloseHandle(proc);
    if(!ok)return false;
    const WCHAR* filename=wcsrchr(path,L'\\');
    filename=filename?filename+1:path;
    return _wcsicmp(filename,L"KakaoTalkUI.exe")==0;
}
BOOL CALLBACK FindQtBetaMain(HWND h, LPARAM param) {
    auto* ctx=reinterpret_cast<BETA_MAIN*>(param);
    WCHAR klass[120]={0};WCHAR title[120]={0};
    if(!IsWindowVisible(h))return TRUE;
    if(!GetClassNameW(h,klass,120) || wcsstr(klass,L"QWindowIcon")==nullptr)return TRUE;
    RECT r;if(!GetWindowRect(h,&r))return TRUE;
    int width=r.right-r.left,height=r.bottom-r.top;
    if(width<350 || width>700 || height<470 || height>1800)return TRUE;
    GetWindowTextW(h,title,120);
    if(wcscmp(title,L"카카오톡")!=0 && wcscmp(title,L"KakaoTalk")!=0 &&
       wcscmp(title,L"カカオトーク")!=0) return TRUE;
    DWORD pid=0;GetWindowThreadProcessId(h,&pid);
    if(!IsBetaProcess(pid))return TRUE;
    ctx->pid=pid;ctx->hwnd=h;ctx->rect=r;
    return FALSE;
}
struct BETA_CHILD_STATE{bool chrome=false;};
BOOL CALLBACK FindBetaChrome(HWND child,LPARAM param) {
    auto* result=reinterpret_cast<BETA_CHILD_STATE*>(param);
    WCHAR klass[120]={0};GetClassNameW(child,klass,120);
    if(wcsncmp(klass,L"Chrome_WidgetWin_",17)==0) {
        result->chrome=true;return FALSE;
    }
    return TRUE;
}
BOOL CALLBACK HideQtBetaAd(HWND h,LPARAM param) {
    auto* main=reinterpret_cast<BETA_MAIN*>(param);
    if(h==main->hwnd)return TRUE;
    DWORD pid=0;GetWindowThreadProcessId(h,&pid);
    if(pid!=main->pid)return TRUE;
    WCHAR klass[120]={0};GetClassNameW(h,klass,120);
    if(wcsstr(klass,L"QWindowIcon")==nullptr)return TRUE;
    RECT r;if(!GetWindowRect(h,&r))return TRUE;
    int mw=main->rect.right-main->rect.left;
    int width=r.right-r.left,height=r.bottom-r.top;
    int gap=8;
    if(abs(r.left-(main->rect.left+gap))>8 ||
       abs(width-(mw-2*gap))>12 ||
       abs(height-100)>14 ||
       abs(main->rect.bottom-(r.bottom+gap))>25)return TRUE;
    BETA_CHILD_STATE child;
    EnumChildWindows(h,FindBetaChrome,reinterpret_cast<LPARAM>(&child));
    if(!child.chrome)return TRUE;
    if(IsWindowVisible(h))ShowWindow(h,SW_HIDE);
    return TRUE;
}
'''
s=s.replace(point,new+'\n'+point,1)
old='''        // Scan ADs recursive
        if (hKakaoTalkMain != NULL) {
            if (bHideMainPannelAd) {
                GetWindowRect(hKakaoTalkMain, &RectKakaoTalkMain);
                FIND_DATA targetClassName = { L"EVA_Window_Dblclk", FALSE };
                EnumWindows(EnumWindowProc, (LPARAM)&targetClassName);
                EnumChildWindows(hKakaoTalkMain, EnumChildProc, NULL);
            }
        }'''
# actual text uses tabs indentation; replace by start/end scan
start=s.index('\t\t// Scan ADs recursive')
end=s.index('\n\t\tbreak;',start)
replacement='''        // Original EVA behavior is retained for stable KakaoTalk 4.x.
        if (hKakaoTalkMain != NULL && bHideMainPannelAd) {
            GetWindowRect(hKakaoTalkMain, &RectKakaoTalkMain);
            FIND_DATA targetClassName = {L"EVA_Window_Dblclk", FALSE};
            EnumWindows(EnumWindowProc,(LPARAM)&targetClassName);
            EnumChildWindows(hKakaoTalkMain,EnumChildProc,NULL);
        }
        // New Qt6 beta path: retain ONLY safe original-style ad window hiding.
        // Qt6 QQuickItems (ChatRoomList/ContactList) aren't HWND child windows.
        // Never resize or clip the KakaoTalk main HWND to fake a longer list.
        if(bHideMainPannelAd) {
            BETA_MAIN beta;
            EnumWindows(FindQtBetaMain,reinterpret_cast<LPARAM>(&beta));
            if(beta.hwnd)EnumWindows(HideQtBetaAd,reinterpret_cast<LPARAM>(&beta));
        }'''
s=s[:start]+replacement+s[end:]
p.write_text(s,encoding='utf-8-sig')
print('Patched source',p, 'bytes',p.stat().st_size)
