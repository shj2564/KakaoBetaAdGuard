from pathlib import Path
p=Path(r'C:\Users\SoNG-MaIN\AppData\Local\SoNG-KakaoBetaAdGuard\KakaoTalkAdGuard_BetaFork\KakaoTalkAdGuard\main.cpp')
s=p.read_text(encoding='utf-8-sig')
backup=p.with_suffix('.before-clip.cpp')
backup.write_text(s,encoding='utf-8-sig')
s=s.replace('#include "framework.h"','#include "framework.h"\n#include <dwmapi.h>\n#pragma comment(lib,"dwmapi.lib")',1)
add=r'''
// Beta-only cosmetic clipping. This does not resize Qt QML items.
// Trial first; restore the entire region/style on exit or disable.
struct BetaVisualClip {
 HWND main=nullptr;
 LONG_PTR initialStyle=0;
 bool styleSaved=false;
 int mainW=0, mainH=0, crop=0;
 bool clipped=false;
};
static BetaVisualClip gBetaVisual;
static bool gBetaAdMeasured=false;
static int gBetaAdHeight=0;
void RestoreBetaVisual() {
 if(gBetaVisual.main && IsWindow(gBetaVisual.main)) {
    HWND h=gBetaVisual.main;
    SetWindowRgn(h,nullptr,TRUE);
    if(gBetaVisual.styleSaved) {
        SetWindowLongPtrW(h,GWL_STYLE,gBetaVisual.initialStyle);
        SetWindowPos(h,nullptr,0,0,0,0,
            SWP_NOSIZE|SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_FRAMECHANGED);
    }
    int borderPref=0;
    DwmSetWindowAttribute(h,33,&borderPref,sizeof(borderPref));
 }
 gBetaVisual=BetaVisualClip{};
 gBetaAdMeasured=false;gBetaAdHeight=0;
}
void ClipQtBetaWhitespace(HWND h) {
 if(!h || !IsWindow(h) || IsZoomed(h) || !gBetaAdMeasured)return;
 RECT r;if(!GetWindowRect(h,&r))return;
 int w=r.right-r.left, ht=r.bottom-r.top;
 if(w<300||ht<400)return;
 int crop=gBetaAdHeight+14;
 if(crop<55||crop>160||ht-crop<320)return;
 if(gBetaVisual.main && gBetaVisual.main!=h)RestoreBetaVisual();
 if(gBetaVisual.clipped && gBetaVisual.main==h &&
    gBetaVisual.mainW==w && gBetaVisual.mainH==ht &&
    gBetaVisual.crop==crop)return;
 if(!gBetaVisual.styleSaved){
    gBetaVisual.initialStyle=GetWindowLongPtrW(h,GWL_STYLE);
    gBetaVisual.styleSaved=true;
 }
 // On builds where DWM cannot move the bottom border to the clipped edge,
 // remove the native frame. This avoids the phantom transparent boundary.
 LONG_PTR oldStyle=GetWindowLongPtrW(h,GWL_STYLE);
 LONG_PTR noFrame=oldStyle & ~(WS_CAPTION|WS_THICKFRAME);
 if(oldStyle!=noFrame) {
    SetWindowLongPtrW(h,GWL_STYLE,noFrame);
    SetWindowPos(h,nullptr,0,0,0,0,
        SWP_NOSIZE|SWP_NOMOVE|SWP_NOZORDER|SWP_NOACTIVATE|SWP_FRAMECHANGED);
 }
 int squareCorner=1;
 DwmSetWindowAttribute(h,33,&squareCorner,sizeof(squareCorner));
 HRGN rg=CreateRectRgn(0,0,w,ht-crop);
 if(!rg)return;
 if(!SetWindowRgn(h,rg,TRUE)){DeleteObject(rg);return;}
 gBetaVisual.main=h;gBetaVisual.mainW=w;gBetaVisual.mainH=ht;
 gBetaVisual.crop=crop;gBetaVisual.clipped=true;
}
'''
point='// Magnetic window edges without resizing the Qt main HWND.'
assert point in s
s=s.replace(point,add+'\n'+point,1)
s=s.replace('''        if(IsWindowVisible(h))ShowWindow(h,SW_HIDE);
    }
    return TRUE;
}
VOID RestoreBetaBanner()''','''        if(IsWindowVisible(h))ShowWindow(h,SW_HIDE);
        gBetaAdMeasured=true;gBetaAdHeight=height;
    }
    return TRUE;
}
VOID RestoreBetaBanner()''',1)
# Prior restore is defined before globals; insert declaration before it.
s=s.replace('''VOID RestoreBetaBanner() {
    BETA_MAIN beta;''','''VOID RestoreBetaVisual();
VOID RestoreBetaBanner() {
    RestoreBetaVisual();
    BETA_MAIN beta;''',1)
s=s.replace('''    int w=r.right-r.left,h=r.bottom-r.top;
    const int marginX=8,''','''    int w=r.right-r.left,h=r.bottom-r.top;
    if(gBetaVisual.main==hwnd && gBetaVisual.clipped)
        h-=gBetaVisual.crop;
    const int marginX=8,''',1)
s=s.replace('''            if(beta.hwnd)EnumWindows(HideQtBetaAd,reinterpret_cast<LPARAM>(&beta));
        }
        // Independent''','''            if(beta.hwnd) {
                EnumWindows(HideQtBetaAd,reinterpret_cast<LPARAM>(&beta));
                ClipQtBetaWhitespace(beta.hwnd);
            }
        } else {
            RestoreBetaVisual();
        }
        // Independent''',1)
p.write_text(s,encoding='utf-8-sig')
print('Experimental clip code created',p.stat().st_size)
