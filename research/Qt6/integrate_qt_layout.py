from pathlib import Path
import shutil
root=Path(r'C:\Users\SoNG-MaIN\AppData\Local\SoNG-KakaoBetaAdGuard')
p=root/'KakaoTalkAdGuard_BetaFork'/'KakaoTalkAdGuard'/'main.cpp'
s=p.read_text(encoding='utf-8-sig')
backup=root/'Backups'/'qt6_fullheight_research'/'main_legacy_clip.cpp'
backup.parent.mkdir(parents=True,exist_ok=True)
shutil.copy2(p,backup)
mark='VOID RestoreBetaBanner() {'
assert mark in s and 'ClipQtBetaWhitespace(beta.hwnd);' in s
hook=r'''
// Qt6 native layout reflow: the existing 100px advertisement loader and 1px
// separator are hidden inside the KakaoTalk GUI thread. Qt ColumnLayout then
// gives the complete original 677px main content height to StackView.
// This does NOT enlarge, clip, or overlay the KakaoTalk window.
static HMODULE gQtLayoutModule=nullptr;
static HHOOK gQtLayoutHook=nullptr;
static HWND gQtLayoutMain=nullptr;
static DWORD gQtLayoutSentTick=0;
static void StopQt6LayoutHook() {
 if(gQtLayoutHook) {
    if(gQtLayoutMain && IsWindow(gQtLayoutMain)) {
        PostMessageW(gQtLayoutMain,WM_APP+101,0,0);
        Sleep(240); // Give KakaoTalk UI thread a chance to restore the layout.
    }
    UnhookWindowsHookEx(gQtLayoutHook);
    gQtLayoutHook=nullptr;
 }
 if(gQtLayoutModule){FreeLibrary(gQtLayoutModule);gQtLayoutModule=nullptr;}
 gQtLayoutMain=nullptr;gQtLayoutSentTick=0;
}
static void EnsureQt6LayoutHook(HWND mainWindow) {
 if(!mainWindow || !IsWindow(mainWindow))return;
 if(gQtLayoutHook && gQtLayoutMain==mainWindow) {
    if(GetTickCount()-gQtLayoutSentTick>6000) {
       PostMessageW(mainWindow,WM_APP+100,0,0);
       gQtLayoutSentTick=GetTickCount();
    }
    return;
 }
 if(gQtLayoutHook)StopQt6LayoutHook();
 wchar_t self[MAX_PATH]={};
 if(!GetModuleFileNameW(nullptr,self,MAX_PATH))return;
 std::wstring libPath(self);
 auto slash=libPath.find_last_of(L"\\/");
 if(slash==std::wstring::npos)return;
 libPath.resize(slash+1);
 libPath+=L"SoNGQt6Layout.dll";
 HMODULE dll=LoadLibraryW(libPath.c_str());
 if(!dll)return;
 auto callback=(HOOKPROC)GetProcAddress(dll,"QtProbeHook");
 DWORD process=0;
 DWORD tid=GetWindowThreadProcessId(mainWindow,&process);
 if(!callback || !tid){FreeLibrary(dll);return;}
 HHOOK hook=SetWindowsHookExW(WH_GETMESSAGE,callback,dll,tid);
 if(!hook){FreeLibrary(dll);return;}
 gQtLayoutModule=dll;gQtLayoutHook=hook;gQtLayoutMain=mainWindow;
 gQtLayoutSentTick=GetTickCount();
 PostMessageW(mainWindow,WM_APP+100,0,0);
}
'''
s=s.replace(mark,hook+'\n'+mark,1)
s=s.replace('VOID RestoreBetaBanner() {\n    RestoreBetaVisual();','VOID RestoreBetaBanner() {\n    StopQt6LayoutHook();\n    RestoreBetaVisual();',1)
s=s.replace('                ClipQtBetaWhitespace(beta.hwnd);','                EnsureQt6LayoutHook(beta.hwnd);',1)
s=s.replace('        } else {\n            RestoreBetaVisual();\n        }\n        // Independent','        } else {\n            StopQt6LayoutHook();\n            RestoreBetaVisual();\n        }\n        // Independent',1)
assert 'EnsureQt6LayoutHook(beta.hwnd);' in s
assert 'StopQt6LayoutHook();\n    RestoreBetaVisual();' in s
p.write_text(s,encoding='utf-8-sig')
print('Source integrated',p,'backup',backup)
