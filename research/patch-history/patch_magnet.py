from pathlib import Path
p=Path(r'C:\Users\SoNG-MaIN\AppData\Local\SoNG-KakaoBetaAdGuard\KakaoTalkAdGuard_BetaFork\KakaoTalkAdGuard\main.cpp')
s=p.read_text(encoding='utf-8-sig')
marker='VOID CALLBACK TimerProc(HWND hwnd, UINT message, UINT idEvent, DWORD dwTimer) {'
assert marker in s
block=r'''
// Magnetic window edges without resizing the Qt main HWND. Unlike the
// experimental .NET helper, this leaves its real size equal to its DWM size.
static HWND betaLastMain=nullptr;
static RECT betaLastPosition={0};
static DWORD betaLastMovement=0;
static bool betaSnapPending=true;
void KeepQtBetaInsideWorkArea(HWND hwnd) {
    if(!hwnd || IsIconic(hwnd) || IsZoomed(hwnd))return;
    RECT r;if(!GetWindowRect(hwnd,&r))return;
    if(hwnd!=betaLastMain) {
        betaLastMain=hwnd;betaLastPosition=r;
        betaLastMovement=GetTickCount();betaSnapPending=true;return;
    }
    if(r.left!=betaLastPosition.left || r.top!=betaLastPosition.top ||
       r.right!=betaLastPosition.right || r.bottom!=betaLastPosition.bottom) {
        betaLastPosition=r;betaLastMovement=GetTickCount();betaSnapPending=true;return;
    }
    if(!betaSnapPending || (GetAsyncKeyState(VK_LBUTTON)&0x8000))return;
    if(GetTickCount()-betaLastMovement<150)return;
    betaSnapPending=false;
    HMONITOR mon=MonitorFromWindow(hwnd,MONITOR_DEFAULTTONEAREST);
    MONITORINFO info={sizeof(info)};
    if(!GetMonitorInfoW(mon,&info))return;
    RECT area=info.rcWork;
    int w=r.right-r.left,h=r.bottom-r.top;
    const int marginX=8, marginTop=1, marginBottom=8;
    int leftMin=area.left-marginX;
    int leftMax=area.right-(w-marginX);
    int topMin=area.top-marginTop;
    int topMax=area.bottom-(h-marginBottom);
    if(leftMax<leftMin)leftMax=leftMin;
    if(topMax<topMin)topMax=topMin;
    int x=max(leftMin,min(leftMax,r.left));
    int y=max(topMin,min(topMax,r.top));
    const int reach=22;
    if(abs(x-leftMin)<=reach)x=leftMin;
    else if(abs(x-leftMax)<=reach)x=leftMax;
    if(abs(y-topMin)<=reach)y=topMin;
    else if(abs(y-topMax)<=reach)y=topMax;
    if(x!=r.left || y!=r.top) {
        SetWindowPos(hwnd,NULL,x,y,0,0,
            SWP_NOSIZE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_NOOWNERZORDER);
        betaLastPosition.left=x;betaLastPosition.top=y;
        betaLastPosition.right=x+w;betaLastPosition.bottom=y+h;
    }
}
'''
s=s.replace(marker,block+'\n'+marker,1)
needle='''            if(beta.hwnd)EnumWindows(HideQtBetaAd,reinterpret_cast<LPARAM>(&beta));
        }'''
repl='''            if(beta.hwnd)EnumWindows(HideQtBetaAd,reinterpret_cast<LPARAM>(&beta));
        }
        // Independent from the advertisement setting: keep Qt main fully
        // inside its monitor's work area and magnetically align near edges.
        BETA_MAIN currentBeta;
        EnumWindows(FindQtBetaMain,reinterpret_cast<LPARAM>(&currentBeta));
        if(currentBeta.hwnd)KeepQtBetaInsideWorkArea(currentBeta.hwnd);'''
assert needle in s
s=s.replace(needle,repl,1)
p.write_text(s,encoding='utf-8-sig')
print('Original C++ beta fork: added minimal native magnetic docking')
